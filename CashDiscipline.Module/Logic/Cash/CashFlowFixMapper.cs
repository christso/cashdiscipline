﻿using CashDiscipline.Module.BusinessObjects.Cash;
using CashDiscipline.Module.BusinessObjects.Forex;
using CashDiscipline.Module.Controllers.Forex;
using CashDiscipline.Module.ParamObjects.Cash;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Xpo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using SmartFormat;

namespace CashDiscipline.Module.Logic.Cash
{
    public class CashFlowFixMapper
    {
        private readonly XPObjectSpace objSpace;
        private IList<CashFlowFixMapping> maps;

        #region SQL Templates

        private const string MapCommandTextListSqlTemplate =
            MapCommandTextListSqlTemplateCommon + @"
AND CashFlow.TranDate BETWEEN @FromDate AND @ToDate
AND CashFlow.[Snapshot] = @Snapshot
AND (
    CashFlow.Fix IS NULL OR Fix.Name LIKE 'Auto' 
    OR CashFlow.FixActivity IS NULL
    OR CashFlow.ForexSettleType IS NULL
    OR CashFlow.ForexSettleType = @AutoForexSettleType
)";

        private const string MapCommandTextListByCashFlowSqlTemplate =
            MapCommandTextListSqlTemplateCommon + @"
AND CashFlow.[Oid] IN ({OidQuery})";

        private const string MapCommandTextListSqlTemplateCommon = @"UPDATE CashFlow SET
{SetText}
FROM CashFlow
LEFT JOIN CashFlowSource Source ON Source.Oid = CashFlow.Source
LEFT JOIN Activity ON Activity.Oid = CashFlow.Activity
LEFT JOIN Account ON Account.Oid = CashFlow.Account
LEFT JOIN CashForecastFixTag Fix ON Fix.Oid = CashFlow.Fix
WHERE CashFLow.GCRecord IS NULL";

        public string ParameterCommandText
        {
            get
            {
                var sqlStringUtil = new SqlStringUtil();
                return sqlStringUtil.CreateCommandText(SqlDeclareClauses);
            }
        }

        public readonly List<SqlDeclareClause> SqlDeclareClauses;

        public List<SqlDeclareClause> CreateSqlDeclareClauses()
        {
            var clauses = new List<SqlDeclareClause>()
            {
                new SqlDeclareClause("FromDate", "date", "(SELECT TOP 1 FromDate FROM CashFlowFixParam WHERE GCRecord IS NULL)"),
                new SqlDeclareClause("ToDate", "date", "(SELECT TOP 1 ToDate FROM CashFlowFixParam WHERE GCRecord IS NULL)"),
                new SqlDeclareClause("IgnoreFixTagType", "int", "0"),
                new SqlDeclareClause("AllocateFixTagType", "int", "1"),
                new SqlDeclareClause("ScheduleInFixTagType", "int", "2"),
                new SqlDeclareClause("ScheduleOutFixTagType", "int", "3"),
                new SqlDeclareClause("ForecastStatus", "int", "0"),
                new SqlDeclareClause("Snapshot", "uniqueidentifier", @"COALESCE(
	(SELECT TOP 1 [Snapshot] FROM CashFlowFixParam WHERE GCRecord IS NULL),
	(SELECT TOP 1 [CurrentCashFlowSnapshot] FROM SetOfBooks WHERE GCRecord IS NULL)
)"),
                new SqlDeclareClause("DefaultCounterparty", "uniqueidentifier",
                    "(SELECT TOP 1 [Counterparty] FROM CashFlowDefaults WHERE GCRecord IS NULL)"),
                new SqlDeclareClause("ReversalFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'R' AND GCRecord IS NULL)"),
                new SqlDeclareClause("RevRecFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'RR' AND GCRecord IS NULL)"),
                new SqlDeclareClause("ResRevRecFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'RRR' AND GCRecord IS NULL)"),
                new SqlDeclareClause("PayrollFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'PR' AND GCRecord IS NULL)"),
                new SqlDeclareClause("AutoFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'Auto' AND GCRecord IS NULL)"),
                new SqlDeclareClause("ApReclassActivity", "uniqueidentifier",
                    "(SELECT TOP 1 ApReclassActivity FROM CashFlowFixParam WHERE GCRecord IS NULL)"),
                new SqlDeclareClause("UndefActivity", "uniqueidentifier",
                    "(select oid from activity where activity.name like 'UNDEFINED' and GCRecord IS NULL)"),
                new SqlDeclareClause("AutoForexSettleType", "int", Convert.ToString(
                        Convert.ToInt32(CashFlowForexSettleType.Auto)))

            };
            return clauses;
        }
        #endregion

        public CashFlowFixMapper(XPObjectSpace objSpace)
        {
            this.objSpace = objSpace;

            var sqlStringUtil = new SqlStringUtil();
            this.SqlDeclareClauses = CreateSqlDeclareClauses();
        }

        public void Process(IEnumerable cashFlows)
        {
            RefreshMaps();

            var oidStrings = new List<string>();
            foreach (CashFlow cashFlow in cashFlows)
            {
                oidStrings.Add(string.Format("'{0}'", cashFlow.Oid));
            }

            var conn = (SqlConnection)objSpace.Session.Connection;
            var command = conn.CreateCommand();
            var commandTextList = GetMapCommandTextListByItem(cashFlows);

            foreach (string commandText in commandTextList)
            {
                command.CommandText = ParameterCommandText + "\n\n" + commandText;
                command.ExecuteNonQuery();
            }
        }

        public void ProcessByOids(string oidQuery)
        {
            RefreshMaps();

            var conn = (SqlConnection)objSpace.Session.Connection;
            var command = conn.CreateCommand();
            var commandTextList = GetMapCommandTextListByOids(oidQuery);

            foreach (string commandText in commandTextList)
            {
                command.CommandText = ParameterCommandText + "\n\n" + commandText;
                command.ExecuteNonQuery();
            }
        }

        public void Process()
        {
            RefreshMaps();

            var conn = (SqlConnection)objSpace.Session.Connection;
            var command = conn.CreateCommand();
            var commandTextList = GetMapCommandTextList();

            foreach (string commandText in commandTextList)
            {
                command.CommandText = ParameterCommandText + "\n\n" + commandText;
                command.Parameters.Clear();
                //command.Parameters.AddRange(parameters.ToArray());
                command.ExecuteNonQuery();
            }
        }

        public List<string> GetMapCommandTextListByItem(IEnumerable cashFlows)
        {
            var oids = new List<string>();
            foreach (CashFlow cf in cashFlows)
            {
                oids.Add(string.Format("'{0}'", cf.Oid));
            }
            var mapTextList = GetMapCommandTextListByOids(string.Join(",", oids));
            return mapTextList;
        }

        public List<string> GetMapCommandTextListByOids(string oidQuery)
        {
            var steps = maps.GroupBy(m => new { m.MapStep })
               .OrderBy(g => g.Key.MapStep)
               .Select(g => g.Key.MapStep)
               .ToList<int>();

            var mapTextList = new List<string>();

            foreach (var step in steps)
            {
                var setTextList = GetMapSetCommandTextList(step);

                string setText = string.Join(",\n", setTextList);

                if (setTextList.Count > 0)
                {
                    mapTextList.Add(Smart.Format(MapCommandTextListByCashFlowSqlTemplate,
                        new { SetText = setText, OidQuery = oidQuery }));
                }
            }
            return mapTextList;
        }

        public List<string> GetMapSetCommandTextList(int step)
        {
            var setTextList = new List<string>();

            var commandText = GetMapSetCommandText("FixActivity", m => 
                {
                    if (m.FixActivity != null)
                        return string.Format("'{0}'", m.FixActivity.Oid);
                    else
                        return string.Format("{0}", m.FixActivityExpr);
                },
                m => m.FixActivity != null || !string.IsNullOrWhiteSpace(m.FixActivityExpr), 
                step,
                "WHEN CashFlow.FixActivity IS NOT NULL AND CashFlow.FixActivity <> @UndefActivity AND CashFlow.Fix != @AutoFixTag THEN CashFlow.FixActivity"
                + "\nWHEN (CashFlow.FixActivity IS NULL AND CashFlow.FixActivity = @UndefActivity) AND CashFlow.Fix != @AutoFixTag THEN CashFlow.Activity");
            if (!string.IsNullOrWhiteSpace(commandText))
                setTextList.Add(commandText);

            commandText = GetMapSetCommandText("Fix", m => string.Format("'{0}'", m.Fix.Oid), m => m.Fix != null && m.Fix.Name != "Auto", step,
                "WHEN CashFlow.Fix IS NOT NULL AND CashFlow.Fix != @AutoFixTag THEN CashFlow.Fix");
            if (!string.IsNullOrWhiteSpace(commandText))
                setTextList.Add(commandText);

            commandText = GetMapSetCommandText("FixRank", m => string.Format("'{0}'", m.FixRank),
                m => m.FixRank != 0, step,
                "WHEN CashFlow.{PropertyName} IS NOT NULL AND CashFlow.{PropertyName} != 0 THEN CashFlow.{PropertyName}");
            if (!string.IsNullOrWhiteSpace(commandText))
                setTextList.Add(commandText);

            commandText = GetMapSetCommandText("FixFromDate", m => string.Format("{0}", m.FixFromDateExpr), m => !string.IsNullOrEmpty(m.FixFromDateExpr), step,
                "WHEN CashFlow.FixFromDate IS NOT NULL AND CashFlow.Fix != @AutoFixTag THEN CashFlow.FixFromDate");
            if (!string.IsNullOrWhiteSpace(commandText))
                setTextList.Add(commandText);

            commandText = GetMapSetCommandText("FixToDate", m => string.Format("{0}", m.FixToDateExpr), m => !string.IsNullOrEmpty(m.FixToDateExpr), step,
                "WHEN CashFlow.FixToDate IS NOT NULL AND CashFlow.Fix != @AutoFixTag THEN CashFlow.FixToDate");
            if (!string.IsNullOrWhiteSpace(commandText))
                setTextList.Add(commandText);

            // skip settle type = EXCLUDE and AUTO
            commandText = GetMapSetCommandText("ForexSettleType", m => string.Format("{0}", Convert.ToInt32(m.ForexSettleType)),
                m => m.ForexSettleType != CashFlowForexSettleType.Auto, step,
                "WHEN CashFlow.ForexSettleType IS NOT NULL AND CashFlow.ForexSettleType != @AutoForexSettleType THEN CashFlow.ForexSettleType");
            if (!string.IsNullOrWhiteSpace(commandText))
                setTextList.Add(commandText);

            commandText = "DateUnfix = CASE WHEN CashFlow.DateUnfix IS NULL THEN CashFlow.TranDate ELSE CashFlow.DateUnfix END";
            setTextList.Add(commandText);

            return setTextList;
        }

        // e.g. mapPropertyName = "FixActivity"; mapPropertyValue = "Handset Pchse"
        public List<string> GetMapCommandTextList()
        {
            var steps = maps.GroupBy(m => new { m.MapStep })
                .OrderBy(g => g.Key.MapStep)
                .Select(g => g.Key.MapStep)
                .ToList<int>();

            var mapTextList = new List<string>();

            foreach (var step in steps)
            {
                var setTextList = GetMapSetCommandTextList(step);

                string setText = string.Join(",\n", setTextList);

                if (setTextList.Count > 0)
                {
                    mapTextList.Add(Smart.Format(MapCommandTextListSqlTemplate,
                        new { SetText = setText }));
                }
            }
            return mapTextList;
        }

        public IList<CashFlowFixMapping> RefreshMaps()
        {
            this.maps = objSpace.GetObjects<CashFlowFixMapping>();
            return maps;
        }

        // defaultValueSql: The first condition in the CASE statement.
        public string GetMapSetCommandText(string mapPropertyName,
            Func<CashFlowFixMapping, string> mapPropertyValue, 
            Predicate<CashFlowFixMapping> predicate, int step, 
            string defaultValueSql)
        {
            ValidateMapExists();
            if (mapPropertyValue == null)
                throw new ArgumentException("mapPropertyValue");
            if (predicate == null)
                throw new ArgumentException("predicate");

            string elseValue = string.Empty;
            var mapsCmdList = new List<string>();

            foreach (var map in maps.Where(m =>
                m.MapStep == step && predicate(m))
                .OrderBy(m => m.RowIndex))
            {
                if (map.CriteriaExpression.ToLower().Trim() == "else")
                {
                    elseValue = mapPropertyValue(map);
                }
                else
                {
                    mapsCmdList.Add(string.Format(
                        @"WHEN {0} THEN {1}",
                        map.CriteriaExpression, mapPropertyValue(map)));
                }
            }

            // add ELSE clause if value is specified in the mapping tables
            if (!string.IsNullOrEmpty(elseValue))
                mapsCmdList.Add("ELSE " + elseValue);

            // join list elements into single string
            string setText = string.Empty;

            if (mapsCmdList.Count > 0)
            {
                if (string.IsNullOrEmpty(defaultValueSql))
                    defaultValueSql = "WHEN CashFlow.{PropertyName} IS NOT NULL THEN CashFlow.{PropertyName}";

                var mapsCmdText = string.Join("\n", mapsCmdList);
                mapsCmdText = Smart.Format(defaultValueSql, new { PropertyName = mapPropertyName })
                    + "\n" + mapsCmdText;

                setText = string.Format(@"{1} = CASE
{0}
END",
mapsCmdText,
mapPropertyName);
            }
            return setText;
        }

        private void ValidateMapExists()
        {
            if (this.maps == null)
                throw new InvalidOperationException("Maps cannot be null");
        }


    }
}
