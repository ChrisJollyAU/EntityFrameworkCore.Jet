using LibRed;
using LibRed.Engine;
using Xunit;

namespace LibRed.Engine.Tests;

// Referential integrity on UPDATE/DELETE + the ON UPDATE/ON DELETE actions, all verified against ACE:
// NO ACTION rejects, CASCADE propagates, ON DELETE SET NULL nulls the child FK (Jet has no ON UPDATE SET NULL).
public class ReferentialActionTests
{
    private static string Fresh()
    {
        string path = TemporaryDatabase.CopyPath(Path.Combine(AppContext.BaseDirectory, "Data", "Northwind.accdb"), "ri-");
        return path;
    }

    private static QueryEngine SetUp(JetDatabase db, string fkClause)
    {
        var e = new QueryEngine(db);
        e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY, N long)");
        e.ExecuteNonQuery($"CREATE TABLE C (Id long PRIMARY KEY, ParentId long, CONSTRAINT FK_C FOREIGN KEY (ParentId) REFERENCES P (Id){fkClause})");
        e.ExecuteNonQuery("INSERT INTO P (Id, N) VALUES (1, 10)");
        e.ExecuteNonQuery("INSERT INTO P (Id, N) VALUES (2, 20)");
        e.ExecuteNonQuery("INSERT INTO C (Id, ParentId) VALUES (100, 1)"); // two children of P#1
        e.ExecuteNonQuery("INSERT INTO C (Id, ParentId) VALUES (101, 1)");
        return e;
    }

    private static void Run(string fkClause, Action<QueryEngine> act)
    {
        string path = Fresh();
        try { using var db = JetDatabase.Open(path, readOnly: false); act(SetUp(db, fkClause)); }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static int[] ChildParents(QueryEngine e) =>
        e.ExecuteQuery("SELECT ParentId FROM C ORDER BY Id").Rows.Select(r => r[0] is null ? -1 : Convert.ToInt32(r[0])).ToArray();

    [Fact]
    public void No_action_rejects_deleting_or_key_updating_a_parent_with_children()
    {
        Run("", e =>
        {
            Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("DELETE FROM P WHERE Id = 1"));
            Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("UPDATE P SET Id = 9 WHERE Id = 1"));
            Assert.Equal(2, e.ExecuteQuery("SELECT Id FROM P").Rows.Count()); // nothing changed
            Assert.Equal(new[] { 1, 1 }, ChildParents(e));

            // A parent with NO children can be deleted / key-updated freely.
            Assert.Equal(1, e.ExecuteNonQuery("DELETE FROM P WHERE Id = 2"));
        });
    }

    [Fact]
    public void Child_fk_update_must_reference_an_existing_parent()
    {
        Run("", e =>
        {
            Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("UPDATE C SET ParentId = 99 WHERE Id = 100"));
            Assert.Equal(1, e.ExecuteNonQuery("UPDATE C SET ParentId = 2 WHERE Id = 100")); // 2 exists → allowed
            Assert.Equal(new[] { 2, 1 }, ChildParents(e));
        });
    }

    [Fact]
    public void Cascade_delete_removes_the_children()
    {
        Run(" ON DELETE CASCADE", e =>
        {
            Assert.Equal(1, e.ExecuteNonQuery("DELETE FROM P WHERE Id = 1"));
            Assert.Empty(e.ExecuteQuery("SELECT Id FROM C").Rows);                 // both children gone
            Assert.Equal(new[] { 2 }, e.ExecuteQuery("SELECT Id FROM P").Rows.Select(r => Convert.ToInt32(r[0])));
        });
    }

    [Fact]
    public void Cascade_update_rewrites_the_children_fk()
    {
        Run(" ON UPDATE CASCADE", e =>
        {
            Assert.Equal(1, e.ExecuteNonQuery("UPDATE P SET Id = 9 WHERE Id = 1"));
            Assert.Equal(new[] { 9, 9 }, ChildParents(e));                          // children followed the new key
            Assert.Equal(9, Convert.ToInt32(e.ExecuteQuery("SELECT Id FROM P WHERE N = 10").Rows.Single()[0]));
        });
    }

    [Fact]
    public void Set_null_delete_nulls_the_children_fk()
    {
        Run(" ON DELETE SET NULL", e =>
        {
            Assert.Equal(1, e.ExecuteNonQuery("DELETE FROM P WHERE Id = 1"));
            Assert.Equal(new[] { -1, -1 }, ChildParents(e));                        // children's ParentId set to NULL
            Assert.Equal(2, e.ExecuteQuery("SELECT Id FROM C").Rows.Count());       // children still there
        });
    }

    // SET NULL on a SELF-referencing table, where the row being nulled and the row being deleted are rows of
    // one table. The action reads each child from the snapshot it took before the delete, so a child that the
    // delete has already removed — or that an earlier child's nulling has already rewritten — is looked up by
    // a key the table no longer holds, and the index says "entry not found".
    [Fact]
    public void Set_null_delete_on_a_self_referencing_table_nulls_the_children()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery(
                "CREATE TABLE T (Id long PRIMARY KEY, ParentId long, "
                + "CONSTRAINT FK_T FOREIGN KEY (ParentId) REFERENCES T (Id) ON DELETE SET NULL)");
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (1, NULL)");   // the root
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (2, 1)");      // three children of it
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (3, 1)");
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (4, 1)");
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (5, 2)");      // and a grandchild

            Assert.Equal(1, e.ExecuteNonQuery("DELETE FROM T WHERE Id = 1"));

            // The root is gone, its three children point at nothing, and the grandchild is untouched.
            var rows = e.ExecuteQuery("SELECT Id, ParentId FROM T ORDER BY Id").Rows
                .Select(r => (Convert.ToInt32(r[0]), r[1] is null ? -1 : Convert.ToInt32(r[1]))).ToArray();
            Assert.Equal([(2, -1), (3, -1), (4, -1), (5, 2)], rows);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // The harder half: the row the action nulls is ALSO one the statement is deleting. Deleting the parent
    // rewrites the child's FK, and the statement then reaches that child carrying the values it read before —
    // whose key no longer names anything in the index.
    [Fact]
    public void Set_null_delete_reaches_a_child_the_same_statement_is_deleting()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery(
                "CREATE TABLE T (Id long PRIMARY KEY, ParentId long, "
                + "CONSTRAINT FK_T FOREIGN KEY (ParentId) REFERENCES T (Id) ON DELETE SET NULL)");
            e.ExecuteNonQuery("CREATE INDEX IX_Parent ON T (ParentId)");
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (1, NULL)");
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (2, 1)");
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (3, 2)");
            e.ExecuteNonQuery("INSERT INTO T (Id, ParentId) VALUES (4, 1)");

            // 1 and 2 go together, and 2 is 1's child — so the action rewrites a row the delete also removes.
            Assert.Equal(2, e.ExecuteNonQuery("DELETE FROM T WHERE Id = 1 OR Id = 2"));

            var rows = e.ExecuteQuery("SELECT Id, ParentId FROM T ORDER BY Id").Rows
                .Select(r => (Convert.ToInt32(r[0]), r[1] is null ? -1 : Convert.ToInt32(r[1]))).ToArray();
            Assert.Equal([(3, -1), (4, -1)], rows);

            // And the index agrees with the rows: a seek on the nulled column finds both survivors.
            Assert.Equal(2, e.ExecuteQuery("SELECT Id FROM T WHERE ParentId IS NULL").Rows.Count());
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ON UPDATE SET NULL is a pathway only — its Jet storage bytes are unverified (ACE's OLE DB provider
    // rejects the DDL), so creating one throws NotImplemented rather than guessing the bytes.
    [Fact]
    public void On_update_set_null_throws_not_implemented()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY)");
            var ex = Assert.Throws<NotImplementedException>(() => e.ExecuteNonQuery(
                "CREATE TABLE C (Id long PRIMARY KEY, ParentId long, CONSTRAINT FK_C FOREIGN KEY (ParentId) REFERENCES P (Id) ON UPDATE SET NULL ON DELETE SET NULL)"));
            Assert.Contains("ON UPDATE SET NULL", ex.Message);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Set_null_action_persists_and_reads_back()
    {
        string path = Fresh();
        try
        {
            using (var db = JetDatabase.Open(path, readOnly: false)) SetUp(db, " ON DELETE SET NULL");
            using var db2 = JetDatabase.Open(path);
            var fk = Assert.Single(db2.Catalog.ForeignKeysOf("C"));
            Assert.True(fk.DeleteSetNull);
            Assert.False(fk.CascadeDelete);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A cascade rewrites a child row, so it owes that row every invariant an UPDATE of it would: SET NULL may
    // not null a Required column, and CASCADE may not drive two children onto one unique key. Table.Update
    // enforces nothing itself, so without these checks the statement — which never names the child table —
    // writes what the UPDATE path a few lines away explicitly refuses.
    [Fact]
    public void Set_null_refuses_to_null_a_required_child_column()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY)");
            e.ExecuteNonQuery("CREATE TABLE C (Id long PRIMARY KEY, ParentId long NOT NULL, "
                + "CONSTRAINT FK_C FOREIGN KEY (ParentId) REFERENCES P (Id) ON DELETE SET NULL)");
            e.ExecuteNonQuery("INSERT INTO P (Id) VALUES (1)");
            e.ExecuteNonQuery("INSERT INTO C (Id, ParentId) VALUES (100, 1)");

            Assert.ThrowsAny<Exception>(() => e.ExecuteNonQuery("DELETE FROM P WHERE Id = 1"));

            // The child row is intact, not half-nulled.
            Assert.Equal(1, Convert.ToInt32(e.ExecuteQuery("SELECT ParentId FROM C WHERE Id = 100").Rows.Single()[0]));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Cascade_refuses_to_create_a_duplicate_child_key()
    {
        Run(" ON UPDATE CASCADE", e =>
        {
            // One child per parent, and a unique index over the FK column: moving parent 1 onto 2 would
            // cascade its child onto the other child's key.
            e.ExecuteNonQuery("DELETE FROM C WHERE Id = 101");
            e.ExecuteNonQuery("INSERT INTO C (Id, ParentId) VALUES (102, 2)");
            e.ExecuteNonQuery("CREATE UNIQUE INDEX UX_C ON C (ParentId)");

            Assert.ThrowsAny<Exception>(() => e.ExecuteNonQuery("UPDATE P SET Id = 2 WHERE Id = 1"));
        });
    }

    // SET NULL may not null a primary-key column either — the same rule an UPDATE of the child meets.
    [Fact]
    public void Set_null_refuses_to_null_a_primary_key_column()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY)");
            e.ExecuteNonQuery("CREATE TABLE C (ParentId long, X long, CONSTRAINT PK_C PRIMARY KEY (ParentId, X), "
                + "CONSTRAINT FK_C FOREIGN KEY (ParentId) REFERENCES P (Id) ON DELETE SET NULL)");
            e.ExecuteNonQuery("INSERT INTO P (Id) VALUES (1)");
            e.ExecuteNonQuery("INSERT INTO C (ParentId, X) VALUES (1, 1)");

            var error = Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("DELETE FROM P WHERE Id = 1"));
            Assert.Contains("cannot contain a Null value", error.Message, StringComparison.Ordinal);
            Assert.Equal(1, Convert.ToInt32(e.ExecuteQuery("SELECT ParentId FROM C").Rows.Single()[0]));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A child whose key a cascade rewrites is a parent in its own right: A -> B -> C, each ON UPDATE CASCADE over
    // B's unique Aid, carries A's new key all the way down.
    [Fact]
    public void Cascade_update_continues_down_a_chain()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = ChainOf(db, " ON UPDATE CASCADE", " ON UPDATE CASCADE");

            e.ExecuteNonQuery("UPDATE A SET Id = 5");

            Assert.Equal(5, Convert.ToInt32(e.ExecuteQuery("SELECT Aid FROM B").Rows.Single()[0]));
            Assert.Equal(5, Convert.ToInt32(e.ExecuteQuery("SELECT Baid FROM C").Rows.Single()[0]));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ON DELETE SET NULL changes the child's key, so the rows referencing THAT key follow their own rule: a
    // cascade takes them to null too, and NO ACTION refuses — neither leaves a row pointing at a key that is gone.
    [Fact]
    public void Set_null_onto_a_referenced_key_cascades_to_its_children()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = ChainOf(db, " ON DELETE SET NULL", " ON UPDATE CASCADE");

            e.ExecuteNonQuery("DELETE FROM A");

            Assert.Null(e.ExecuteQuery("SELECT Aid FROM B").Rows.Single()[0]);
            Assert.Null(e.ExecuteQuery("SELECT Baid FROM C").Rows.Single()[0]);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Fact]
    public void Set_null_onto_a_referenced_key_with_no_action_children_is_refused()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = ChainOf(db, " ON DELETE SET NULL", "");

            var error = Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("DELETE FROM A"));
            Assert.Contains("table 'C' includes related records", error.Message, StringComparison.Ordinal);
            Assert.Equal(1, Convert.ToInt32(e.ExecuteQuery("SELECT Aid FROM B").Rows.Single()[0]));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A DELETE whose rows refer to each other, on schemas and data ACE was run on. Each row is what ACE returned:
    // the rows deleted, or the error, and then what is left. ACE deletes row by row in the order it reads the rows
    // (by Id when the whole WHERE is ranges on the primary key, else in file order; through a join, in the order the
    // join reaches the target), each row's relationships in the stored order of the parent's incoming blocks, and a
    // NO ACTION child holds its parent only while the statement has not yet deleted it.
    private const string SelfRef = "CREATE TABLE T (Id long PRIMARY KEY, P long, CONSTRAINT FK_P FOREIGN KEY (P) REFERENCES T (Id))";
    private const string TwoSelfRefs = "CREATE TABLE T (Id long PRIMARY KEY, P long, Q long, "
        + "CONSTRAINT FK_P FOREIGN KEY (P) REFERENCES T (Id), CONSTRAINT FK_Q FOREIGN KEY (Q) REFERENCES T (Id) ON DELETE CASCADE)";
    private const string Related = "The record cannot be deleted or changed because table 'T' includes related records.";
    private const string Cascading = "Cannot perform cascading operation. Since related records exist in table 'T', "
        + "referential integrity rules would be violated.";
    private static readonly string[] Pf = [SelfRef, "INSERT INTO T VALUES (1, NULL)", "INSERT INTO T VALUES (2, 1)"];
    private static readonly string[] PfRev = [SelfRef, "INSERT INTO T VALUES (2, NULL)", "INSERT INTO T VALUES (1, 2)"];
    private static readonly string[] Cf = [SelfRef, "INSERT INTO T VALUES (1, NULL)", "INSERT INTO T VALUES (2, NULL)", "UPDATE T SET P = 2 WHERE Id = 1"];
    private static readonly string[] CfRev = [SelfRef, "INSERT INTO T VALUES (2, NULL)", "INSERT INTO T VALUES (1, NULL)", "UPDATE T SET P = 1 WHERE Id = 2"];
    private static readonly string[] WithX = ["CREATE TABLE X (Id long PRIMARY KEY)", "INSERT INTO X VALUES (1)", "INSERT INTO X VALUES (2)"];
    private static readonly string[] Third = ["INSERT INTO T VALUES (3, NULL)"];

    public static TheoryData<string, string[], string, int?, string?, string> AceDeletes => new()
    {
        { "pf", Pf, "DELETE FROM T", 2, null, "" },
        { "pfRev", PfRev, "DELETE FROM T", 2, null, "" },
        { "cf", Cf, "DELETE FROM T", 2, null, "" },
        { "cfRev", CfRev, "DELETE FROM T", 2, null, "" },
        { "pf", Pf, "DELETE FROM T WHERE 1 = 1", null, Related, "1,2" },
        { "pfRev", PfRev, "DELETE FROM T WHERE 1 = 1", null, Related, "1,2" },
        { "cf", Cf, "DELETE FROM T WHERE 1 = 1", 2, null, "" },
        { "cfRev", CfRev, "DELETE FROM T WHERE 1 = 1", 2, null, "" },
        { "pf", Pf, "DELETE FROM T WHERE Id > 0", null, Related, "1,2" },
        { "pfRev", PfRev, "DELETE FROM T WHERE Id > 0", 2, null, "" },
        { "cf", Cf, "DELETE FROM T WHERE Id > 0", 2, null, "" },
        { "cfRev", CfRev, "DELETE FROM T WHERE Id > 0", null, Related, "1,2" },
        { "pf", Pf, "DELETE FROM T WHERE Id + 0 > 0", null, Related, "1,2" },
        { "pfRev", PfRev, "DELETE FROM T WHERE Id + 0 > 0", null, Related, "1,2" },
        { "cf", Cf, "DELETE FROM T WHERE Id + 0 > 0", 2, null, "" },
        { "cfRev", CfRev, "DELETE FROM T WHERE Id + 0 > 0", 2, null, "" },
        { "pf", Pf, "DELETE FROM T WHERE Id IN (1, 2)", null, Related, "1,2" },
        { "pfRev", PfRev, "DELETE FROM T WHERE Id IN (1, 2)", null, Related, "1,2" },
        { "cf", Cf, "DELETE FROM T WHERE Id IN (1, 2)", 2, null, "" },
        { "cfRev", CfRev, "DELETE FROM T WHERE Id IN (1, 2)", 2, null, "" },
        { "pf", Pf, "DELETE * FROM T WHERE P IS NULL OR P IS NOT NULL", null, Related, "1,2" },
        { "pfRev", PfRev, "DELETE * FROM T WHERE P IS NULL OR P IS NOT NULL", null, Related, "1,2" },
        { "cf", Cf, "DELETE * FROM T WHERE P IS NULL OR P IS NOT NULL", 2, null, "" },
        { "cfRev", CfRev, "DELETE * FROM T WHERE P IS NULL OR P IS NOT NULL", 2, null, "" },
        { "pf", Pf, "DELETE FROM T WHERE P > 0 OR P IS NULL", null, Related, "1,2" },
        { "pfRev", PfRev, "DELETE FROM T WHERE P > 0 OR P IS NULL", null, Related, "1,2" },
        { "pf", Pf, "DELETE FROM T WHERE Id BETWEEN 1 AND 2", null, Related, "1,2" },
        { "pfRev", PfRev, "DELETE FROM T WHERE Id BETWEEN 1 AND 2", 2, null, "" },
        { "pf+X", [.. Pf, .. WithX], "DELETE T.* FROM T INNER JOIN X ON T.Id = X.Id", null, Related, "1,2" },
        { "pf+X", [.. Pf, .. WithX], "DELETE T.* FROM X INNER JOIN T ON T.Id = X.Id", null, Related, "1,2" },
        { "pf+3", [.. Pf, .. Third], "DELETE FROM T WHERE Id IN (1, 2)", null, Related, "1,2,3" },
        { "pf+3", [.. Pf, .. Third], "DELETE FROM T WHERE Id < 3", null, Related, "1,2,3" },
        { "pfRev+3", [.. PfRev, .. Third], "DELETE FROM T WHERE Id < 3", 2, null, "3" },
        { "pf+3", [.. Pf, .. Third], "DELETE FROM T WHERE Id > 0 AND (P > 0 OR P IS NULL)", null, Related, "1,2,3" },
        { "self-loop", [SelfRef, "INSERT INTO T VALUES (1, NULL)", "UPDATE T SET P = 1"], "DELETE FROM T", 1, null, "" },
        { "self-loop", [SelfRef, "INSERT INTO T VALUES (1, NULL)", "UPDATE T SET P = 1"], "DELETE FROM T WHERE Id = 1", 1, null, "" },
        { "self-loop", [SelfRef, "INSERT INTO T VALUES (1, NULL)", "UPDATE T SET P = 1"], "DELETE FROM T WHERE Id + 0 = 1", 1, null, "" },
        { "pf, child in C", [.. Pf, "CREATE TABLE C (Id long PRIMARY KEY, T long, CONSTRAINT FK_C FOREIGN KEY (T) REFERENCES T (Id))",
            "INSERT INTO C VALUES (10, 2)"], "DELETE FROM T", null, Related, "1,2" },
        { "two self-refs", [TwoSelfRefs, "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, 1, NULL)",
            "INSERT INTO T VALUES (3, NULL, 1)"], "DELETE FROM T", 3, null, "" },
        { "cascade-reached self-loop", [TwoSelfRefs, "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, NULL, 1)",
            "UPDATE T SET P = 2 WHERE Id = 2"], "DELETE FROM T WHERE Id = 1", 1, null, "" },
        { "cascade-reached self-loop", [TwoSelfRefs, "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, NULL, 1)",
            "UPDATE T SET P = 2 WHERE Id = 2"], "DELETE FROM T", 2, null, "" },
        { "cascade reaches a later root", [TwoSelfRefs, "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, NULL, NULL)",
            "INSERT INTO T VALUES (3, 2, 1)"], "DELETE FROM T WHERE 1 = 1", null, "Record is deleted.", "1,2,3" },
        { "cascade-reached row held", [TwoSelfRefs, "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (4, NULL, NULL)",
            "INSERT INTO T VALUES (3, NULL, 1)", "UPDATE T SET P = 3 WHERE Id = 4"], "DELETE FROM T WHERE 1 = 1", null, Cascading, "1,3,4" },
        { "held through C", ["CREATE TABLE T (Id long PRIMARY KEY, X long)",
            "CREATE TABLE C (Id long PRIMARY KEY, T long, CONSTRAINT FK_CT FOREIGN KEY (T) REFERENCES T (Id) ON DELETE CASCADE)",
            "ALTER TABLE T ADD CONSTRAINT FK_TX FOREIGN KEY (X) REFERENCES C (Id)",
            "INSERT INTO T VALUES (1, NULL)", "INSERT INTO C VALUES (10, 1)", "INSERT INTO T VALUES (2, 10)"],
            "DELETE FROM T", null, Cascading, "1,2" },
        { "held through C", ["CREATE TABLE T (Id long PRIMARY KEY, X long)",
            "CREATE TABLE C (Id long PRIMARY KEY, T long, CONSTRAINT FK_CT FOREIGN KEY (T) REFERENCES T (Id) ON DELETE CASCADE)",
            "ALTER TABLE T ADD CONSTRAINT FK_TX FOREIGN KEY (X) REFERENCES C (Id)",
            "INSERT INTO T VALUES (1, NULL)", "INSERT INTO C VALUES (10, 1)", "INSERT INTO T VALUES (2, 10)"],
            "DELETE FROM T WHERE 1 = 1", null, Cascading, "1,2" },
        // Two CASCADE self-relationships and a NO ACTION one, declared A, B, H: 1's cascade deletes 2, and 2's
        // deletes 3, before H is looked at, whichever of 2 and 3 holds the other through H.
        { "A, B cascade, H no action", [CascadesThenHold, "INSERT INTO T VALUES (1, NULL, NULL, NULL)",
            "INSERT INTO T VALUES (2, 1, NULL, NULL)", "INSERT INTO T VALUES (3, 2, 1, NULL)"], "DELETE FROM T WHERE Id = 1", 1, null, "" },
        { "A, B cascade, H no action; 2 held by 3", [CascadesThenHold, "INSERT INTO T VALUES (1, NULL, NULL, NULL)",
            "INSERT INTO T VALUES (2, 1, NULL, NULL)", "INSERT INTO T VALUES (3, 2, 1, NULL)", "UPDATE T SET H = 3 WHERE Id = 2"],
            "DELETE FROM T WHERE Id = 1", 1, null, "" },
        { "A, B cascade, H no action; 3 held by 2", [CascadesThenHold, "INSERT INTO T VALUES (1, NULL, NULL, NULL)",
            "INSERT INTO T VALUES (2, 1, NULL, NULL)", "INSERT INTO T VALUES (3, 2, 1, NULL)", "UPDATE T SET H = 2 WHERE Id = 3"],
            "DELETE FROM T WHERE Id = 1", 1, null, "" },
        // A row held through NO ACTION P and deleted through CASCADE Q: which relationship was declared first decides.
        { "P declared before Q", [TwoSelfRefs, "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, 1, 1)"],
            "DELETE FROM T WHERE Id = 1", null, Related, "1,2" },
        { "Q declared before P", ["CREATE TABLE T (Id long PRIMARY KEY, P long, Q long, "
            + "CONSTRAINT FK_Q FOREIGN KEY (Q) REFERENCES T (Id) ON DELETE CASCADE, CONSTRAINT FK_P FOREIGN KEY (P) REFERENCES T (Id))",
            "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, 1, 1)"], "DELETE FROM T WHERE Id = 1", 1, null, "" },
        // Dropped and added again, a relationship takes back its place among the parent's incoming blocks, and that
        // place, not MSysRelationships' row order, is the order ACE applies them in.
        { "P dropped and added again", [TwoSelfRefs, "ALTER TABLE T DROP CONSTRAINT FK_P",
            "ALTER TABLE T ADD CONSTRAINT FK_P FOREIGN KEY (P) REFERENCES T (Id)",
            "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, 1, 1)"], "DELETE FROM T WHERE Id = 1", null, Related, "1,2" },
        { "Q dropped and added again", [TwoSelfRefs, "ALTER TABLE T DROP CONSTRAINT FK_Q",
            "ALTER TABLE T ADD CONSTRAINT FK_Q FOREIGN KEY (Q) REFERENCES T (Id) ON DELETE CASCADE",
            "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, 1, 1)"], "DELETE FROM T WHERE Id = 1", null, Related, "1,2" },
        // The NO ACTION's incoming block numbered 2 (.rC), the cascade's 17 (.rBB): stored first, the cascade goes first.
        { "incoming blocks 2 and 17", IncomingTwoAndSeventeen, "DELETE FROM T WHERE Id = 1", 1, null, "" },
        { "names reversed", ["CREATE TABLE T (Id long PRIMARY KEY, P long, Q long, CONSTRAINT Z_P FOREIGN KEY (P) REFERENCES T (Id), "
            + "CONSTRAINT A_Q FOREIGN KEY (Q) REFERENCES T (Id) ON DELETE CASCADE)",
            "INSERT INTO T VALUES (1, NULL, NULL)", "INSERT INTO T VALUES (2, 1, 1)"], "DELETE FROM T WHERE Id = 1", null, Related, "1,2" },
        { "cascade-reached row held by a row that stays", [TwoSelfRefs, "INSERT INTO T VALUES (1, NULL, NULL)",
            "INSERT INTO T VALUES (2, 1, NULL)", "INSERT INTO T VALUES (3, NULL, 1)", "UPDATE T SET P = 3 WHERE Id = 2"],
            "DELETE FROM T WHERE Id = 1", null, Cascading, "1,2,3" },
        // T 1 cascades to C 10, which cascades to T 2: the scan then reaches T 2 deleted, with a WHERE or without,
        // as another table references T.
        { "T and C cascading round", CascadeRound, "DELETE FROM T", null, "Record is deleted.", "1,2" },
        { "T and C cascading round", CascadeRound, "DELETE FROM T WHERE 1 = 1", null, "Record is deleted.", "1,2" },
        // SET NULL onto a required column of a row the statement deletes later: nulled first, so refused — unless
        // the DELETE has no WHERE and empties the table.
        { "SET NULL onto a required root", RequiredSetNull, "DELETE FROM T WHERE Id IN (1, 2)", null, RequiredNull, "1,2" },
        { "SET NULL onto a required root", RequiredSetNull, "DELETE FROM T WHERE 1 = 1", null, RequiredNull, "1,2" },
        { "SET NULL onto a required root", RequiredSetNull, "DELETE FROM T", 2, null, "" },
        { "SET NULL down a chain", ["CREATE TABLE T (Id long PRIMARY KEY, P long, CONSTRAINT f FOREIGN KEY (P) REFERENCES T (Id) ON DELETE SET NULL)",
            "INSERT INTO T VALUES (1, NULL)", "INSERT INTO T VALUES (2, 1)", "INSERT INTO T VALUES (3, 2)"],
            "DELETE FROM T WHERE Id IN (1, 2)", 2, null, "3" },
    };

    private static readonly string[] IncomingTwoAndSeventeen =
    [
        "CREATE TABLE T (Id long PRIMARY KEY, P long, Q long, CONSTRAINT FK_P FOREIGN KEY (P) REFERENCES T (Id))",
        .. Enumerable.Range(0, 13).SelectMany(i => new[] { $"ALTER TABLE T ADD COLUMN C{i} long", $"CREATE INDEX IX{i:00} ON T (C{i})" }),
        "ALTER TABLE T ADD CONSTRAINT FK_Q FOREIGN KEY (Q) REFERENCES T (Id) ON DELETE CASCADE",
        "INSERT INTO T (Id, P, Q) VALUES (1, NULL, NULL)", "INSERT INTO T (Id, P, Q) VALUES (2, 1, 1)",
    ];

    // ACE: ON UPDATE CASCADE of a unique key set to Null, into a required child column, is refused in its words for a
    // cascade.
    [Fact]
    public void An_update_cascade_writing_null_into_a_required_column_is_refused_as_a_cascade()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY, U long UNIQUE)");
            e.ExecuteNonQuery("CREATE TABLE C (Id long PRIMARY KEY, u long NOT NULL, CONSTRAINT fc FOREIGN KEY (u) REFERENCES P (U) ON UPDATE CASCADE)");
            e.ExecuteNonQuery("INSERT INTO P VALUES (1, 5)");
            e.ExecuteNonQuery("INSERT INTO C VALUES (10, 5)");
            Assert.Equal("Cannot perform cascading operation. It would cause field 'C.u' to become Null, which is not allowed.",
                Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("UPDATE P SET U = NULL")).Message);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE: P held by C1 and C2, both NO ACTION; a key UPDATE and a DELETE of P both name C1 — also once C1's
    // relationship is dropped and added again, as it takes back its place among P's incoming blocks.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_refused_parent_names_the_first_relationship_in_its_incoming_blocks(bool addedAgain)
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY)");
            e.ExecuteNonQuery("CREATE TABLE C1 (Id long PRIMARY KEY, p long, CONSTRAINT f1 FOREIGN KEY (p) REFERENCES P (Id))");
            e.ExecuteNonQuery("CREATE TABLE C2 (Id long PRIMARY KEY, p long, CONSTRAINT f2 FOREIGN KEY (p) REFERENCES P (Id))");
            if (addedAgain)
            {
                e.ExecuteNonQuery("ALTER TABLE C1 DROP CONSTRAINT f1");
                e.ExecuteNonQuery("ALTER TABLE C1 ADD CONSTRAINT f1 FOREIGN KEY (p) REFERENCES P (Id)");
            }
            e.ExecuteNonQuery("INSERT INTO P VALUES (1)");
            e.ExecuteNonQuery("INSERT INTO C1 VALUES (10, 1)");
            e.ExecuteNonQuery("INSERT INTO C2 VALUES (20, 1)");
            const string error = "The record cannot be deleted or changed because table 'C1' includes related records.";
            Assert.Equal(error, Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("UPDATE P SET Id = 9")).Message);
            Assert.Equal(error, Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("DELETE FROM P WHERE Id = 1")).Message);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private static readonly string[] RequiredSetNull = [
        "CREATE TABLE T (Id long PRIMARY KEY, P long NOT NULL, CONSTRAINT f FOREIGN KEY (P) REFERENCES T (Id) ON DELETE SET NULL)",
        "INSERT INTO T VALUES (1, 1)", "INSERT INTO T VALUES (2, 1)"];
    private const string RequiredNull = "Cannot perform cascading operation. It would cause field 'T.P' to become Null, which is not allowed.";

    // ACE: A(1); C(5) and C(6) cascade from A 1; D(1) cascades from C 5; C.x also references D with SET NULL. Deleting
    // A 1 deletes C 5, then D 1, whose SET NULL clears C 6's x — so C 6 no longer holds A 1 and stays, with x Null.
    [Fact]
    public void A_cascade_child_detached_by_an_earlier_rule_is_left()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE A (Id long PRIMARY KEY)");
            e.ExecuteNonQuery("CREATE TABLE C (Id long PRIMARY KEY, x long, CONSTRAINT fca FOREIGN KEY (x) REFERENCES A (Id) ON DELETE CASCADE)");
            e.ExecuteNonQuery("CREATE TABLE D (Id long PRIMARY KEY, c long, CONSTRAINT fdc FOREIGN KEY (c) REFERENCES C (Id) ON DELETE CASCADE)");
            e.ExecuteNonQuery("INSERT INTO A VALUES (1)");
            e.ExecuteNonQuery("INSERT INTO C VALUES (5, NULL)");
            e.ExecuteNonQuery("INSERT INTO D VALUES (1, 5)");
            e.ExecuteNonQuery("UPDATE C SET x = 1 WHERE Id = 5");
            e.ExecuteNonQuery("ALTER TABLE C ADD CONSTRAINT fcd FOREIGN KEY (x) REFERENCES D (Id) ON DELETE SET NULL");
            e.ExecuteNonQuery("INSERT INTO C VALUES (6, 1)");
            Assert.Equal(1, e.ExecuteNonQuery("DELETE FROM A WHERE Id = 1"));
            var row = e.ExecuteQuery("SELECT Id, x FROM C").Rows.Single();
            Assert.Equal(6, Convert.ToInt32(row[0]));
            Assert.Null(row[1]);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE: P(1); S holds it with SET NULL, declared first, and N with NO ACTION. S is nulled, then N refuses — and the
    // statement leaves S as it was.
    [Fact]
    public void A_set_null_already_applied_goes_back_when_a_later_rule_refuses()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY)");
            e.ExecuteNonQuery("CREATE TABLE S (Id long PRIMARY KEY, p long, CONSTRAINT fs FOREIGN KEY (p) REFERENCES P (Id) ON DELETE SET NULL)");
            e.ExecuteNonQuery("CREATE TABLE N (Id long PRIMARY KEY, p long, CONSTRAINT fn FOREIGN KEY (p) REFERENCES P (Id))");
            e.ExecuteNonQuery("INSERT INTO P VALUES (1)");
            e.ExecuteNonQuery("INSERT INTO S VALUES (20, 1)");
            e.ExecuteNonQuery("INSERT INTO N VALUES (10, 1)");
            Assert.Equal("The record cannot be deleted or changed because table 'N' includes related records.",
                Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("DELETE FROM P WHERE Id = 1")).Message);
            Assert.Equal(1, Convert.ToInt32(e.ExecuteQuery("SELECT p FROM S").Rows.Single()[0]));
            Assert.Single(e.ExecuteQuery("SELECT Id FROM P").Rows);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    private const string CascadesThenHold = "CREATE TABLE T (Id long PRIMARY KEY, A long, B long, H long, "
        + "CONSTRAINT FK_A FOREIGN KEY (A) REFERENCES T (Id) ON DELETE CASCADE, "
        + "CONSTRAINT FK_B FOREIGN KEY (B) REFERENCES T (Id) ON DELETE CASCADE, CONSTRAINT FK_H FOREIGN KEY (H) REFERENCES T (Id))";
    private static readonly string[] CascadeRound = ["CREATE TABLE T (Id long PRIMARY KEY, X long)",
        "CREATE TABLE C (Id long PRIMARY KEY, T long, CONSTRAINT FK_CT FOREIGN KEY (T) REFERENCES T (Id) ON DELETE CASCADE)",
        "ALTER TABLE T ADD CONSTRAINT FK_TX FOREIGN KEY (X) REFERENCES C (Id) ON DELETE CASCADE",
        "INSERT INTO T VALUES (1, NULL)", "INSERT INTO C VALUES (10, 1)", "INSERT INTO T VALUES (2, 10)"];

    // ACE: P(1), P(2); N holds P 1 (NO ACTION), S holds P 2 (SET NULL onto a required column). The roots go in the
    // order read and each one's rules run before the next root: P 1 first refuses for N, P 2 first fails S's null.
    [Theory]
    [InlineData(1, 2, "The record cannot be deleted or changed because table 'N' includes related records.")]
    [InlineData(2, 1, "Cannot perform cascading operation. It would cause field 'S.p' to become Null, which is not allowed.")]
    public void Each_root_has_its_rules_applied_before_the_next_root(int first, int second, string error)
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY)");
            e.ExecuteNonQuery("CREATE TABLE N (Id long PRIMARY KEY, p long, CONSTRAINT fn FOREIGN KEY (p) REFERENCES P (Id))");
            e.ExecuteNonQuery("CREATE TABLE S (Id long PRIMARY KEY, p long NOT NULL, CONSTRAINT fs FOREIGN KEY (p) REFERENCES P (Id) ON DELETE SET NULL)");
            e.ExecuteNonQuery($"INSERT INTO P VALUES ({first})");
            e.ExecuteNonQuery($"INSERT INTO P VALUES ({second})");
            e.ExecuteNonQuery("INSERT INTO N VALUES (10, 1)");
            e.ExecuteNonQuery("INSERT INTO S VALUES (20, 2)");
            Assert.Equal(error, Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery("DELETE FROM P WHERE 1 = 1")).Message);
            Assert.Equal(2, e.ExecuteQuery("SELECT Id FROM P").Rows.Count());
            Assert.Equal(2, Convert.ToInt32(e.ExecuteQuery("SELECT p FROM S").Rows.Single()[0]));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE: A(1); B(1) cascades from A 1; C(5) holds A 1 (NO ACTION) and B 1 (SET NULL) through one column x, B's
    // relationship to A declared first. Deleting A 1 deletes B 1, which nulls C.x, so nothing holds A 1.
    [Fact]
    public void A_set_null_run_earlier_frees_the_no_action_parent()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE A (Id long PRIMARY KEY)");
            e.ExecuteNonQuery("CREATE TABLE B (Id long PRIMARY KEY, a long, CONSTRAINT fba FOREIGN KEY (a) REFERENCES A (Id) ON DELETE CASCADE)");
            e.ExecuteNonQuery("CREATE TABLE C (Id long PRIMARY KEY, x long, CONSTRAINT fca FOREIGN KEY (x) REFERENCES A (Id), "
                + "CONSTRAINT fcb FOREIGN KEY (x) REFERENCES B (Id) ON DELETE SET NULL)");
            e.ExecuteNonQuery("INSERT INTO A VALUES (1)");
            e.ExecuteNonQuery("INSERT INTO B VALUES (1, 1)");
            e.ExecuteNonQuery("INSERT INTO C VALUES (5, 1)");
            Assert.Equal(1, e.ExecuteNonQuery("DELETE FROM A WHERE Id = 1"));
            Assert.Null(e.ExecuteQuery("SELECT x FROM C").Rows.Single()[0]);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    [Theory]
    [MemberData(nameof(AceDeletes))]
    public void A_delete_of_rows_that_refer_to_each_other_matches_ace(
        string setup, string[] sql, string delete, int? deleted, string? error, string left)
    {
        _ = setup; // names the data in the test explorer
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            foreach (string s in sql) e.ExecuteNonQuery(s);
            if (error is null)
                Assert.Equal(deleted, e.ExecuteNonQuery(delete));
            else
                Assert.Equal(error, Assert.Throws<InvalidOperationException>(() => e.ExecuteNonQuery(delete)).Message);
            Assert.Equal(left, string.Join(",", e.ExecuteQuery("SELECT Id FROM T ORDER BY Id").Rows.Select(r => r[0])));
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // ACE: P(1); B(20) and A(10) both cascade from P, B's relationship made first; B holds A through a NO ACTION key.
    // `DELETE FROM P WHERE Id = 1` deletes P and, by the cascades, B then A: B is gone before A, so nothing holds A.
    [Fact]
    public void A_cascade_deleting_the_no_action_child_first_lets_its_parent_go()
    {
        string path = Fresh();
        try
        {
            using var db = JetDatabase.Open(path, readOnly: false);
            var e = new QueryEngine(db);
            e.ExecuteNonQuery("CREATE TABLE P (Id long PRIMARY KEY)");
            e.ExecuteNonQuery("CREATE TABLE B (Id long PRIMARY KEY, P long, CONSTRAINT fbp FOREIGN KEY (P) REFERENCES P (Id) ON DELETE CASCADE)");
            e.ExecuteNonQuery("CREATE TABLE A (Id long PRIMARY KEY, P long, CONSTRAINT fap FOREIGN KEY (P) REFERENCES P (Id) ON DELETE CASCADE)");
            e.ExecuteNonQuery("ALTER TABLE B ADD COLUMN A long");
            e.ExecuteNonQuery("ALTER TABLE B ADD CONSTRAINT fba FOREIGN KEY (A) REFERENCES A (Id)");
            e.ExecuteNonQuery("INSERT INTO P VALUES (1)");
            e.ExecuteNonQuery("INSERT INTO A (Id, P) VALUES (10, 1)");
            e.ExecuteNonQuery("INSERT INTO B (Id, P, A) VALUES (20, 1, 10)");
            Assert.Equal(1, e.ExecuteNonQuery("DELETE FROM P WHERE Id = 1"));
            Assert.Empty(e.ExecuteQuery("SELECT Id FROM A").Rows);
            Assert.Empty(e.ExecuteQuery("SELECT Id FROM B").Rows);
        }
        finally { TemporaryDatabase.Delete(path); }
    }

    // A(Id) <- B(Aid, unique) <- C(Baid), one row each, all holding key 1.
    private static QueryEngine ChainOf(JetDatabase db, string bRule, string cRule)
    {
        var e = new QueryEngine(db);
        e.ExecuteNonQuery("CREATE TABLE A (Id long PRIMARY KEY)");
        e.ExecuteNonQuery($"CREATE TABLE B (Id long PRIMARY KEY, Aid long, CONSTRAINT FK_B FOREIGN KEY (Aid) REFERENCES A (Id){bRule})");
        e.ExecuteNonQuery("CREATE UNIQUE INDEX UX_B ON B (Aid)");
        e.ExecuteNonQuery($"CREATE TABLE C (Id long PRIMARY KEY, Baid long, CONSTRAINT FK_C FOREIGN KEY (Baid) REFERENCES B (Aid){cRule})");
        e.ExecuteNonQuery("INSERT INTO A (Id) VALUES (1)");
        e.ExecuteNonQuery("INSERT INTO B (Id, Aid) VALUES (1, 1)");
        e.ExecuteNonQuery("INSERT INTO C (Id, Baid) VALUES (1, 1)");
        return e;
    }
}
