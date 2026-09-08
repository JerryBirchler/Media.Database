using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Queries;

[TestFixture]
public class QueryRegistrationsTests
{
    [Test]
    public void AddBySourceInformationSql_Should_Contain_Insert_Values_Returning()
    {
        var sql = QueryRegistrations.AddBySourceInformationSql;
        sql.ShouldContain("INSERT INTO");
        sql.ShouldContain("VALUES");
        sql.ShouldContain("RETURNING");
    }

    [Test]
    public void UpdateSourceInformationSql_Should_Contain_Update_Where_Returning()
    {
        var sql = QueryRegistrations.UpdateSourceInformationSql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING");
    }

    [Test]
    public void GetBySourceInformationSql_Should_Contain_Select_LeftJoin_Where_Limit1()
    {
        var sql = QueryRegistrations.GetBySourceInformationSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("LEFT JOIN");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("LIMIT 1");
    }

    [Test]
    public void GetBySourceMachineUuidSql_Should_Contain_Select_LeftJoin_Where_Limit1()
    {
        var sql = QueryRegistrations.GetBySourceMachineUuidSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("LEFT JOIN");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("LIMIT 1");
        sql.ShouldContain("@SourceMachineUuid");
    }

    [Test]
    public void GetBySourceMachineUuidSql_Should_Select_OtpEmail_As_A_Separate_Column_From_UpdatedOn()
    {
        // Regression test for a missing comma that made "UpdatedOn" an implicit alias for the
        // real OtpEmail column, so the query never actually selected OtpEmail.
        var sql = QueryRegistrations.GetBySourceMachineUuidSql;
        sql.ShouldContain("\"UpdatedOn\",");
    }

    [TestCase(nameof(QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql))]
    [TestCase(nameof(QueryRegistrations.VerifyOtpEmailSql))]
    [TestCase(nameof(QueryRegistrations.VerifyOtpCellPhoneSql))]
    public void UpdateFromJoinQuery_Should_Name_The_Target_Table_Before_The_Alias(string queryName)
    {
        // Regression test: "UPDATE r SET ... FROM Registrations AS r ..." is invalid Postgres --
        // the bare "r" right after UPDATE must already be a real relation name, since the alias
        // defined moments later in FROM doesn't exist yet at that point in the statement. Postgres
        // rejects it with "relation "r" does not exist". The target table has to be named directly
        // after UPDATE ("UPDATE "Registrations" AS r"), with FROM introducing only the other
        // (joined) table. Only surfaces against a real connection, since every test here mocks
        // ISqlQueryExecutor.
        var sql = (string)typeof(QueryRegistrations).GetProperty(queryName)!.GetValue(null)!;
        sql.ShouldContain("UPDATE public.\"Registrations\" AS r SET");
        sql.ShouldNotContain("UPDATE r SET");
    }

    [TestCase(nameof(QueryRegistrations.GetBySourceInformationSql))]
    [TestCase(nameof(QueryRegistrations.GetBySourceMachineUuidSql))]
    [TestCase(nameof(QueryRegistrations.GetBySourceMachineIdSql))]
    public void JoinedSelectQuery_Should_Qualify_SourceMachineId_With_Table_Alias(string queryName)
    {
        // Regression test: "SourceMachineId" exists on both joined tables (Registrations and
        // SourceMachineRegistrations). An unqualified reference in the SELECT list is rejected by
        // Postgres at runtime as ambiguous -- only ever surfaces against a real connection, since
        // every test here mocks ISqlQueryExecutor.
        var sql = (string)typeof(QueryRegistrations).GetProperty(queryName)!.GetValue(null)!;
        sql.ShouldContain("smr.\"SourceMachineId\"");
    }

    [TestCase(nameof(QueryRegistrations.GetBySourceInformationSql))]
    [TestCase(nameof(QueryRegistrations.GetBySourceMachineUuidSql))]
    [TestCase(nameof(QueryRegistrations.GetBySourceMachineIdSql))]
    public void JoinedSelectQuery_Should_Reference_A_Properly_Quoted_Id_Column(string queryName)
    {
        // Regression test: "CASE WHEN r.Id IS NULL..." was a hand-typed, unquoted column
        // reference -- Postgres folds unquoted identifiers to lowercase ("r.id"), which doesn't
        // match the real quoted "Id" column, so this failed with "column r.id does not exist" at
        // runtime. Every other column in this file goes through the quoted schema constants; this
        // one didn't.
        var sql = (string)typeof(QueryRegistrations).GetProperty(queryName)!.GetValue(null)!;
        sql.ShouldContain("r.\"Id\" IS NULL");
        sql.ShouldNotContain("r.Id IS NULL");
    }

    [Test]
    public void InactivateRegistrationsBySourceMachineUuidSql_Should_Contain_Update_Where_Returning()
    {
        var sql = QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING");
        sql.ShouldContain("@SourceMachineUuid");
    }

    [Test]
    public void AddRegistrationBySourceMachineUuidSql_Should_Contain_Insert_Select_Where_Returning()
    {
        var sql = QueryRegistrations.AddRegistrationBySourceMachineUuidSql;
        sql.ShouldContain("INSERT INTO");
        sql.ShouldContain("SELECT");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING");
        sql.ShouldContain("@SourceMachineUuid");
    }

    [Test]
    public void AddRegistrationBySourceMachineUuidSql_Should_Contain_A_From_Clause()
    {
        // Regression test: the INSERT...SELECT had no FROM clause at all, so its SELECT-list
        // columns (sourced from SourceMachineRegistrations) referenced no table in scope --
        // Postgres would reject this at runtime; only ever surfaces against a real connection.
        var sql = QueryRegistrations.AddRegistrationBySourceMachineUuidSql;
        sql.ShouldContain("FROM");
    }

    [Test]
    public void VerifyOtpEmailSql_Should_Contain_Update_Where_Returning_OtpEmail()
    {
        var sql = QueryRegistrations.VerifyOtpEmailSql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING");
        sql.ShouldContain("@OtpEmail");
        sql.ShouldContain("@OtpWindowStart");
    }

    [Test]
    public void VerifyOtpCellPhoneSql_Should_Contain_Update_Where_Returning_OtpCellPhone()
    {
        var sql = QueryRegistrations.VerifyOtpCellPhoneSql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING");
        sql.ShouldContain("@OtpCellPhone");
        sql.ShouldContain("@OtpWindowStart");
    }

    [TestCase(nameof(QueryRegistrations.VerifyOtpEmailSql))]
    [TestCase(nameof(QueryRegistrations.VerifyOtpCellPhoneSql))]
    public void VerifyOtpQuery_Should_Qualify_EmailAddress_And_InsertedOn_With_Table_Alias(string queryName)
    {
        // Regression test: EmailAddress/CellPhoneNumber/InsertedOn all exist on both joined
        // tables. Several WHERE-clause references were unqualified, which Postgres rejects at
        // runtime as ambiguous -- only ever surfaces against a real connection.
        var sql = (string)typeof(QueryRegistrations).GetProperty(queryName)!.GetValue(null)!;
        sql.ShouldContain("r.\"EmailAddress\" = smr.\"EmailAddress\"");
        sql.ShouldContain("r.\"InsertedOn\" > @OtpWindowStart");
    }

    [Test]
    public void UpsertRegistrationCql_Should_Contain_Insert_Values()
    {
        var cql = QueryRegistrations.UpsertRegistrationCql;
        cql.ShouldContain("INSERT INTO");
        cql.ShouldContain("VALUES");
    }

    [Test]
    public void UpsertRegistrationCql_Should_Separate_IsActive_From_IsEmailVerified_With_A_Comma()
    {
        // Regression test for a missing comma that made this an invalid CQL statement, never
        // caught because it only ever runs against a mocked ICqlQueryExecutor in tests.
        var cql = QueryRegistrations.UpsertRegistrationCql;
        var isActiveIndex = cql.IndexOf("is_active", StringComparison.Ordinal);
        var isEmailVerifiedIndex = cql.IndexOf("is_email_verified", StringComparison.Ordinal);

        cql.Substring(isActiveIndex, isEmailVerifiedIndex - isActiveIndex).ShouldContain(",");
    }

    // ToSourceMachineRegistration/ToRegistrationIds/ToSourceInformationResponse/ToAddRegistrationResponse
    // (NpgsqlDataReader extensions) are not unit-testable: NpgsqlDataReader is sealed and cannot be mocked,
    // matching the existing exclusion documented in QueryFilesTests. Exercising these mapping methods
    // requires a real reader (integration-test territory), and they are covered indirectly through
    // RegistrationRepositoryTests via the mocked ISqlQueryExecutor seam.
}
