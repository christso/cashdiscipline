﻿
using CashDiscipline.Module.BusinessObjects.Cash;
using CashDiscipline.Module.Logic.SqlMap;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashDiscipline.Module.Logic.Cash
{
    public class BankStmtMapper
    {
        private readonly XPObjectSpace objSpace;
        private Mapper<BankStmtMapping> mapper;

        private const string MapCommandTextListSqlTemplate =
            MapCommandTextListSqlTemplateCommon + @"
AND BankStmt.TranDate BETWEEN @FromDate AND @ToDate";

        private const string MapCommandTextListByObjectSqlTemplate =
            MapCommandTextListSqlTemplateCommon + @"
AND BankStmt.[Oid] IN ({1})";

        private const string MapCommandTextListSqlTemplateCommon = @"UPDATE BankStmt SET
{0}
FROM BankStmt
LEFT JOIN Activity ON Activity.Oid = BankStmt.Activity
LEFT JOIN Account ON Account.Oid = BankStmt.Account
LEFT JOIN BankStmtTranCode TranCode ON TranCode.Oid = BankStmt.TranCode
WHERE BankStmt.GCRecord IS NULL";

        public BankStmtMapper(XPObjectSpace objspace)
        {
            this.objSpace = objspace;
            this.mapper = new Mapper<BankStmtMapping>(objspace, "BankStmt", GetMapSetCommandTextList);
            mapper.MapCommandTextListByObjectSqlTemplate = MapCommandTextListByObjectSqlTemplate;
            mapper.MapCommandTextListSqlTemplate = MapCommandTextListSqlTemplate;
        }

        public List<SqlDeclareClause> CreateSqlParameters()
        {
            var clauses = new List<SqlDeclareClause>()
            {
                new SqlDeclareClause("UndefActivityOid", "uniqueidentifer",
                "(select oid from activity where activity.name like 'UNDEFINED' and GCRecord IS NULL)"),
                new SqlDeclareClause("UndefCounterpartyOid", "uniqueidentifer",
                "(select oid from counterparty where counterparty.name like 'UNDEFINED' and GCRecord IS NULL)")
            };
            return clauses;
        }

        public List<SqlParameter> CreateParameters(List<SqlDeclareClause> clauses)
        {
            var parameters = new List<SqlParameter>();
            using (var cmd = objSpace.Session.Connection.CreateCommand())
            {
                foreach (var clause in clauses)
                {
                    parameters.Add(new SqlParameter(clause.ParameterName, clause.ExecuteScalar(cmd)));
                }
            }
            return parameters;
        }


        public IList<BankStmtMapping> RefreshMaps()
        {
            return mapper.RefreshMaps();
        }

        public void Process(IEnumerable objs)
        {
            var clauses = CreateSqlParameters();
            var sqlParams = CreateParameters(clauses);
            mapper.SqlParameters = sqlParams;
            mapper.Process(objs);
        }
        
        public List<string> GetMapSetCommandTextList(int step)
        {
            var setTextList = new List<string>();

            var commandText = mapper.GetMapSetCommandText("Activity", m => string.Format("'{0}'", m.Activity.Oid), m => m.Activity != null, step,
                "WHEN BankStmt.Activity IS NOT NULL AND BankStmt.Activity <> @UndefActivityOid THEN BankStmt.Activity");
            if (!string.IsNullOrWhiteSpace(commandText))
                setTextList.Add(commandText);

            commandText = mapper.GetMapSetCommandText("Counterparty", m => string.Format("'{0}'", m.Counterparty.Oid), m => m.Counterparty != null, step,
                "WHEN BankStmt.Counterparty IS NOT NULL AND BankStmt.Counterparty <> @UndefCounterpartyOid THEN BankStmt.Counterparty");
            if (!string.IsNullOrWhiteSpace(commandText))
                setTextList.Add(commandText);

            setTextList.Add(@"OraTrxNum = 
CASE WHEN OraTrxNum IS NULL OR OraTrxNum = '' THEN 
FORMAT(TranDate, 'yyyyMMdd') + '-' + RIGHT(Account.BankAccountNumber, 3)
	+ '-' + RIGHT('000' + RTRIM((
	SELECT COUNT(*) FROM BankStmt b2
	WHERE b2.Account = BankStmt.Account
		AND b2.TranDate = BankStmt.TranDate
		AND b2.Oid <= BankStmt.Oid
	)), 3)
ELSE OraTrxNum END");

            return setTextList;
        }
        
    }
}
