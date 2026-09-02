using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;

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

    [Test]
    public void UpsertRegistrationCql_Should_Contain_Insert_Values()
    {
        var cql = QueryRegistrations.UpsertRegistrationCql;
        cql.ShouldContain("INSERT INTO");
        cql.ShouldContain("VALUES");
    }

    // ToSourceMachineRegistration/ToRegistrationIds/ToSourceInformationResponse/ToAddRegistrationResponse
    // (NpgsqlDataReader extensions) are not unit-testable: NpgsqlDataReader is sealed and cannot be mocked,
    // matching the existing exclusion documented in QueryFilesTests. Exercising these mapping methods
    // requires a real reader (integration-test territory), and they are covered indirectly through
    // RegistrationRepositoryTests via the mocked ISqlQueryExecutor seam.
}
