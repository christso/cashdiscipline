﻿using CashDiscipline.Module.BusinessObjects.Forex;
using CashDiscipline.Module.BusinessObjects.Setup;
using CashDiscipline.Module.ParamObjects.Cash;
using Xafology.ExpressApp.Xpo;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using System;
using System.ComponentModel;
using System.Linq;
using Xafology.Spreadsheet.Attributes;
using DevExpress.Persistent.BaseImpl;
using Xafology.ExpressApp.Xpo.Import;
using CashDiscipline.Module.BusinessObjects.Cash;
using CashDiscipline.Module.BusinessObjects;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Diagnostics;

/* Parameters in SQL
DECLARE @FromDate date = (SELECT TOP 1 FromDate FROM CashFlowFixParam)
DECLARE @ToDate date = (SELECT TOP 1 ToDate FROM CashFlowFixParam)
DECLARE @ApayableLockdownDate date = (SELECT TOP 1 ApayableLockdownDate FROM CashFlowFixParam)
DECLARE @ApayableNextLockdownDate date = (SELECT TOP 1 ApayableNextLockdownDate FROM CashFlowFixParam)
DECLARE @IgnoreFixTagType int = 0
DECLARE @AllocateFixTagType int = 1
DECLARE @ScheduleInFixTagType int = 2
DECLARE @ScheduleOutFixTagType int = 3
DECLARE @ForecastStatus int = 0
DECLARE @Snapshot uniqueidentifier = COALESCE(
	(SELECT TOP 1 [Snapshot] FROM CashFlowFixParam),
	(SELECT TOP 1 [CurrentCashFlowSnapshot] FROM SetOfBooks)
)
DECLARE @DefaultCounterparty uniqueidentifier = (SELECT TOP 1 [Counterparty] FROM CashFlowDefaults)
DECLARE @FunctionalCurrency uniqueidentifier = (SELECT TOP 1 [FunctionalCurrency] FROM SetOfBooks)
DECLARE @ReversalFixTag uniqueidentifier = (SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'R')
DECLARE @RevRecFixTag uniqueidentifier = (SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'RR')
DECLARE @ResRevRecFixTag uniqueidentifier = (SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'RRR')
DECLARE @PayrollFixTag uniqueidentifier = (SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'PR')
DECLARE @ApReclassActivity uniqueidentifier = (SELECT TOP 1 ApReclassActivity FROM CashFlowFixParam)
*/

namespace CashDiscipline.Module.Logic.Cash
{
    public class SqlFixCashFlowsAlgorithm : IFixCashFlows
    {
        private XPObjectSpace objSpace;
        private CashFlowFixParam paramObj;
        private Activity paramApReclassActivity;
        private Counterparty defaultCounterparty;
        private CashForecastFixTag reversalFixTag;
        private CashForecastFixTag revRecFixTag;
        private CashForecastFixTag resRevRecFixTag;
        private CashForecastFixTag payrollFixTag;
        private SetOfBooks setOfBooks;
        private CashFlowSnapshot currentSnapshot;
        private CashFlowFixMapper mapper;

        public SqlFixCashFlowsAlgorithm(XPObjectSpace objSpace, CashFlowFixParam paramObj)
            : this(objSpace, paramObj, new CashFlowFixMapper(objSpace))
        {

        }

        public SqlFixCashFlowsAlgorithm(XPObjectSpace objSpace, CashFlowFixParam paramObj, CashFlowFixMapper mapper)
        {
            this.objSpace = objSpace;
            this.paramObj = paramObj;

            if (paramObj.Snapshot == null)
                currentSnapshot = GetCurrentSnapshot(objSpace.Session);
            else
                currentSnapshot = paramObj.Snapshot;

            if (paramObj.ApReclassActivity != null)
                paramApReclassActivity = objSpace.GetObjectByKey<Activity>(objSpace.GetKeyValue(paramObj.ApReclassActivity));

            defaultCounterparty = objSpace.FindObject<Counterparty>(
             CriteriaOperator.Parse("Name LIKE ?", Constants.DefaultFixCounterparty));

            var query = new XPQuery<CashForecastFixTag>(objSpace.Session);

            reversalFixTag = query
                .Where(x => x.Name == Constants.ReversalFixTag).FirstOrDefault();

            revRecFixTag = query
                .Where(x => x.Name == Constants.RevRecFixTag).FirstOrDefault();

            resRevRecFixTag = query
                .Where(x => x.Name == Constants.ResRevRecFixTag).FirstOrDefault();

            payrollFixTag = query
                .Where(x => x.Name == Constants.PayrollFixTag).FirstOrDefault();

            setOfBooks = SetOfBooks.GetInstance(objSpace);
            this.mapper = mapper;
        }

        public void ProcessCashFlows()
        {
            mapper.Process(CreateParameters());
            Rephase(CreateParameters());
            ApplyFix(CreateParameters());
        }

        public List<SqlDeclareClause> CreateSqlParameters()
        {
            var clauses = new List<SqlDeclareClause>()
            {
                new SqlDeclareClause("FromDate", "date", "(SELECT TOP 1 FromDate FROM CashFlowFixParam)"),
                new SqlDeclareClause("ToDate", "date", "(SELECT TOP 1 ToDate FROM CashFlowFixParam)"),
                new SqlDeclareClause("ApayableLockdownDate", "date", "(SELECT TOP 1 ApayableLockdownDate FROM CashFlowFixParam)"),
                new SqlDeclareClause("ApayableNextLockdownDate", "date", "(SELECT TOP 1 ApayableNextLockdownDate FROM CashFlowFixParam)"),
                new SqlDeclareClause("IgnoreFixTagType", "int", "0"),
                new SqlDeclareClause("AllocateFixTagType", "int", "1"),
                new SqlDeclareClause("ScheduleInFixTagType", "int", "2"),
                new SqlDeclareClause("ScheduleOutFixTagType", "int", "3"),
                new SqlDeclareClause("ForecastStatus", "int", "0"),
                new SqlDeclareClause("Snapshot", "uniqueidentifier", @"COALESCE(
	(SELECT TOP 1 [Snapshot] FROM CashFlowFixParam),
	(SELECT TOP 1 [CurrentCashFlowSnapshot] FROM SetOfBooks)
)"),
                new SqlDeclareClause("DefaultCounterparty", "uniqueidentifier",
                    "(SELECT TOP 1 [Counterparty] FROM CashFlowDefaults)"),
                new SqlDeclareClause("FunctionalCurrency", "uniqueidentifier",
                    "(SELECT TOP 1 [FunctionalCurrency] FROM SetOfBooks)"),
                new SqlDeclareClause("ReversalFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'R')"),
                new SqlDeclareClause("RevRecFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'RR')"),
                new SqlDeclareClause("ResRevRecFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'RRR')"),
                new SqlDeclareClause("PayrollFixTag", "uniqueidentifier",
                    "(SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'PR')"),
                new SqlDeclareClause("ApReclassActivity", "uniqueidentifier",
                    "(SELECT TOP 1 ApReclassActivity FROM CashFlowFixParam)")
            };
            return clauses;
        }

        public List<SqlParameter> CreateParameters()
        {
            var parameters = new List<SqlParameter>()
            {
                new SqlParameter("FromDate", paramObj.FromDate),
                new SqlParameter("ToDate", paramObj.ToDate),
                new SqlParameter("Snapshot", currentSnapshot.Oid),
                new SqlParameter("IgnoreFixTagType", Convert.ToInt32(CashForecastFixTagType.Ignore)),
                new SqlParameter("AllocateFixTagType", Convert.ToInt32(CashForecastFixTagType.Allocate)),
                new SqlParameter("ScheduleOutFixTagType", Convert.ToInt32(CashForecastFixTagType.ScheduleOut)),
                new SqlParameter("ScheduleInFixTagType", Convert.ToInt32(CashForecastFixTagType.ScheduleIn)),
                new SqlParameter("ForecastStatus", Convert.ToInt32(CashFlowStatus.Forecast)),
                new SqlParameter("DefaultCounterparty", defaultCounterparty.Oid),
                new SqlParameter("FunctionalCurrency", setOfBooks.FunctionalCurrency.Oid),
                new SqlParameter("ReversalFixTag", reversalFixTag.Oid),
                new SqlParameter("RevRecFixTag", revRecFixTag.Oid),
                new SqlParameter("ResRevRecFixTag", resRevRecFixTag.Oid),
                new SqlParameter("PayrollFixTag", payrollFixTag.Oid),
                new SqlParameter("ApayableLockdownDate", paramObj.ApayableLockdownDate),
                new SqlParameter("ApayableNextLockdownDate", paramObj.ApayableNextLockdownDate),
                new SqlParameter("ApReclassActivity", paramObj.ApReclassActivity.Oid),
            };
            return parameters;
        }

        public void ApplyFix()
        {
            ApplyFix(CreateParameters());
        }

        public void ApplyFix2()
        {
            var commandText = @"DECLARE @FromDate date = (SELECT TOP 1 FromDate FROM CashFlowFixParam)
DECLARE @ToDate date = (SELECT TOP 1 ToDate FROM CashFlowFixParam)
DECLARE @ApayableLockdownDate date = (SELECT TOP 1 ApayableLockdownDate FROM CashFlowFixParam)
DECLARE @ApayableNextLockdownDate date = (SELECT TOP 1 ApayableNextLockdownDate FROM CashFlowFixParam)
DECLARE @IgnoreFixTagType int = 0
DECLARE @AllocateFixTagType int = 1
DECLARE @ScheduleInFixTagType int = 2
DECLARE @ScheduleOutFixTagType int = 3
DECLARE @ForecastStatus int = 0
DECLARE @Snapshot uniqueidentifier = COALESCE(
	(SELECT TOP 1 [Snapshot] FROM CashFlowFixParam),
	(SELECT TOP 1 [CurrentCashFlowSnapshot] FROM SetOfBooks)
)
DECLARE @DefaultCounterparty uniqueidentifier = (SELECT TOP 1 [Counterparty] FROM CashFlowDefaults)
DECLARE @FunctionalCurrency uniqueidentifier = (SELECT TOP 1 [FunctionalCurrency] FROM SetOfBooks)
DECLARE @ReversalFixTag uniqueidentifier = (SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'R')
DECLARE @RevRecFixTag uniqueidentifier = (SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'RR')
DECLARE @ResRevRecFixTag uniqueidentifier = (SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'RRR')
DECLARE @PayrollFixTag uniqueidentifier = (SELECT TOP 1 Oid FROM CashForecastFixTag WHERE Name LIKE 'PR')
DECLARE @ApReclassActivity uniqueidentifier = (SELECT TOP 1 ApReclassActivity FROM CashFlowFixParam)

-- CashFlowsToFix

IF OBJECT_ID('temp_CashFlowsToFix') IS NOT NULL DROP TABLE temp_CashFlowsToFix;

SELECT cf.* INTO temp_CashFlowsToFix
FROM CashFlow cf
LEFT JOIN CashForecastFixTag tag ON tag.Oid = cf.Fix
LEFT JOIN CashFlow fixer ON fixer.Oid = cf.Oid
WHERE
    cf.GCRecord IS NULL
    AND tag.GCRecord IS NULL
    AND cf.TranDate BETWEEN @FromDate AND @ToDate
	AND cf.[Snapshot] = @Snapshot
    AND (cf.Fix = NULL OR tag.FixTagType != @IgnoreFixTagType)
    AND 
	(
		cf.IsFixeeSynced=0 OR cf.IsFixerSynced=0 OR NOT cf.IsFixerFixeesSynced=0
		OR fixer.GCRecord IS NOT NULL
	)
;

-- Delete Existing Fixes

UPDATE cf
SET 
GCRecord = CAST(RAND() * 2147483646 + 1 AS INT)
FROM CashFlow cf
WHERE cf.GCRecord IS NULL
AND cf.[Snapshot] = @Snapshot
AND cf.Fix IN (@ReversalFixTag, @RevRecFixTag, @ResRevRecFixTag)
AND 
(
	ParentCashFlow IN (SELECT cf2.Oid FROM temp_CashFlowsToFix cf2)
	OR cf.ParentCashFlow IN (SELECT cf1.Oid FROM CashFlow cf1 WHERE cf1.GCRecord IS NULL)
	OR cf.ParentCashFlow IN (SELECT cfd.Oid FROM CashFlow cfd WHERE cfd.GCRecord IS NOT NULL)
)

-- FixeeFixer

IF OBJECT_ID('temp_FixeeFixer') IS NOT NULL DROP TABLE temp_FixeeFixer;

SELECT Fixee, Fixer
INTO temp_FixeeFixer
FROM
(
	SELECT
		-- RowNum = 1 if fixee currency equals fixer currency, otherwise may be greater than 1
		ROW_NUMBER() OVER(
			PARTITION BY fixee.Oid 
			ORDER BY CASE WHEN fixeeAccount.Currency = fixerAccount.Currency THEN 0 ELSE 1 END
		) AS RowNum,
		fixee.Oid AS Fixee,
		fixer.Oid AS Fixer,
		fixee.TranDate,
		fixee.Account,
		fixee.AccountCcyAmt,
		fixee.FixFromDate,
		fixee.FixToDate
	FROM temp_CashFlowsToFix fixee
	LEFT JOIN Counterparty fixeeCparty ON fixeeCparty.Oid = fixee.Counterparty
	LEFT JOIN Account fixeeAccount ON fixeeAccount.Oid = fixee.Account
	LEFT JOIN temp_CashFlowsToFix fixer
		ON fixee.TranDate BETWEEN fixer.FixFromDate AND fixer.FixToDate
			AND fixee.FixActivity = fixer.FixActivity
			AND fixer.[Status] = @ForecastStatus
			AND fixer.FixRank > fixee.FixRank
			AND 
			(
				fixer.Counterparty IS NULL OR fixer.Counterparty = @DefaultCounterparty
				OR fixee.Counterparty IS NULL AND fixer.Counterparty IS NULL
				OR fixer.Counterparty = fixeeCparty.FixCounterparty
				OR fixer.Counterparty = fixee.Counterparty
			)
	LEFT JOIN Account fixerAccount 
		ON fixerAccount.Oid = fixer.Account
	WHERE
		fixee.Account IS NOT NULL AND 
		(
			fixeeAccount.FixAccount = fixerAccount.FixAccount
			OR fixee.Account = fixer.Account
		)
) T1
WHERE RowNum = 1

-- Insert Cash Flow Reversal
IF OBJECT_ID('temp_FixReversal') IS NOT NULL DROP TABLE temp_FixReversal;

SELECT cf.*
INTO temp_FixReversal
FROM temp_CashFlowsToFix cf
JOIN temp_FixeeFixer fixeeFixer ON fixeeFixer.Fixee = cf.Oid;

UPDATE revFix SET 
	Oid = NEWID(),
	ParentCashFlow = revFix.Oid,
	TranDate = fixer.TranDate,

	-- if the currencies do not match, use functional currency
	CounterCcy = CASE WHEN revFix.CounterCcy <> fixer.CounterCcy THEN @FunctionalCurrency ELSE revFix.CounterCcy END,
	CounterCcyAmt = CASE WHEN revFix.CounterCcy <> fixer.CounterCcy THEN -revFix.FunctionalCcyAmt ELSE -revFix.CounterCcyAmt END,

	AccountCcyAmt = -revFix.AccountCcyAmt,
	FunctionalCcyAmt = -revFix.FunctionalCcyAmt,
	Fix = @ReversalFixTag,
	Fixer = NULL

FROM temp_FixReversal revFix
LEFT JOIN temp_FixeeFixer fixeeFixer
	ON fixeeFixer.Fixee = revFix.Oid
LEFT JOIN temp_CashFlowsToFix fixer
	ON fixer.Oid = fixeeFixer.Fixer

-- Reversal logic for AP Lockdown (i.e. payroll is excluded)

	-- Fixee.RR: Reclass Fixee Fix into AP Pymt
	-- (i.e. reverse the reversal so net reversal is zero because the total amount of AP is correct)
IF OBJECT_ID('temp_FixRevReclass_Fixee') IS NOT NULL DROP TABLE temp_FixRevReclass_Fixee;

SELECT fixee.*
INTO temp_FixRevReclass_Fixee
FROM CashFlow fixee
JOIN temp_FixReversal fr ON fr.ParentCashFlow = fixee.OID
JOIN CashFlow fixer ON fixer.Oid = fixee.Fixer
JOIN CashForecastFixTag fixerTag ON fixerTag.Oid = fixer.Fix
WHERE fixee.TranDate <= @ApayableLockdownDate 
	AND (@PayrollFixTag IS NULL OR fixee.Fix != @PayrollFixTag)
	AND fixerTag.FixTagType = @AllocateFixTagType

UPDATE frr SET 
ParentCashFlow = Oid,
Oid = NEWID(),
Fix = @RevRecFixTag,
Activity = @ApReclassActivity
FROM temp_FixRevReclass_Fixee frr
;

	-- Fixee.RRR: Restore Fixee Fix to future date

IF OBJECT_ID('temp_FixResRevRec_Fixee') IS NOT NULL DROP TABLE temp_FixResRevRec_Fixee;

SELECT fixee.*
INTO temp_FixResRevRec_Fixee
FROM CashFlow fixee
JOIN temp_FixReversal fr ON fr.ParentCashFlow = fixee.OID
JOIN CashFlow fixer ON fixer.Oid = fixee.Fixer
JOIN CashForecastFixTag fixerTag ON fixerTag.Oid = fixer.Fix
WHERE fixee.TranDate <= @ApayableLockdownDate 
	AND (@PayrollFixTag IS NULL OR fixee.Fix != @PayrollFixTag)
	AND fixerTag.FixTagType = @AllocateFixTagType

UPDATE frrr SET 
ParentCashFlow = Oid,
Oid = NEWID(),
Fix = @ResRevRecFixTag,
Activity = @ApReclassActivity,
AccountCcyAmt = -frrr.AccountCcyAmt,
CounterCcyAmt = -frrr.CounterCcyAmt,
FunctionalCcyAmt = -frrr.FunctionalCcyAmt,
TranDate = @ApayableNextLockdownDate
FROM temp_FixResRevRec_Fixee frrr
;

	-- Fixer.RR: Reverse Fixer Fix into AP Pymt
IF OBJECT_ID('temp_FixRevReclass_Fixer') IS NOT NULL DROP TABLE temp_FixRevReclass_Fixer;

SELECT fixer.*
INTO temp_FixRevReclass_Fixer
FROM CashFlow fixee
JOIN temp_FixReversal fr ON fr.ParentCashFlow = fixee.OID
JOIN CashFlow fixer ON fixer.Oid = fixee.Fixer
JOIN CashForecastFixTag fixerTag ON fixerTag.Oid = fixer.Fix
WHERE fixee.TranDate <= @ApayableLockdownDate 
	AND (@PayrollFixTag IS NULL OR fixee.Fix != @PayrollFixTag)
	AND fixerTag.FixTagType = @AllocateFixTagType

UPDATE frr SET
ParentCashFlow = Oid, 
Oid = NEWID(),
Fix = @RevRecFixTag,
Activity = @ApReclassActivity,
AccountCcyAmt = -AccountCcyAmt,
FunctionalCcyAmt = -FunctionalCcyAmt,
CounterCcyAmt = -CounterCcyAmt,
IsReclass = 1
FROM temp_FixRevReclass_Fixer frr
;

	-- Fixer.RRR: Restore Fixer Fix to future date

IF OBJECT_ID('temp_FixResRevReclass_Fixer') IS NOT NULL DROP TABLE temp_FixResRevReclass_Fixer;

SELECT fixer.*
INTO temp_FixResRevReclass_Fixer
FROM CashFlow fixee
JOIN temp_FixReversal fr ON fr.ParentCashFlow = fixee.OID
JOIN CashFlow fixer ON fixer.Oid = fixee.Fixer
JOIN CashForecastFixTag fixerTag ON fixerTag.Oid = fixer.Fix
WHERE fixee.TranDate <= @ApayableLockdownDate 
	AND (@PayrollFixTag IS NULL OR fixee.Fix != @PayrollFixTag)
	AND fixerTag.FixTagType = @AllocateFixTagType

UPDATE frrr SET
ParentCashFlow = Oid, 
Oid = NEWID(),
Fix = @ResRevRecFixTag,
Activity = @ApReclassActivity,
TranDate = @ApayableNextLockdownDate
FROM temp_FixResRevReclass_Fixer frrr
;

-- Link Fixee Cash Flow to Fixer

UPDATE CashFlow
SET Fixer = fixeeFixer.Fixer
FROM CashFlow cf
	INNER JOIN temp_FixeeFixer fixeeFixer ON cf.Oid = fixeeFixer.Fixee

-- Finalize

INSERT INTO CashFlow
SELECT * FROM temp_FixReversal
UNION ALL
SELECT * FROM temp_FixRevReclass_Fixee
UNION ALL
SELECT * FROM temp_FixRevReclass_Fixer
UNION ALL
SELECT * FROM temp_FixResRevRec_Fixee
UNION ALL
SELECT * FROM temp_FixResRevReclass_Fixer

-- SELECT
/*
SELECT fixeeFixer.*, 
	fixee.TranDate, 
	fixee.Counterparty,
	fixee.AccountCcyAmt,
	fixee.Account,
	fixeeAccount.FixAccount,
	fixee.FixFromDate,
	fixee.FixToDate
FROM temp_FixeeFixer fixeeFixer
LEFT JOIN CashFlow fixee ON fixee.Oid = fixeeFixer.Fixee
LEFT JOIN Account fixeeAccount ON fixee.Account = fixeeAccount.Oid

SELECT * FROM temp_FixReversal
*/";

            
            var conn = (SqlConnection)objSpace.Session.Connection;
            var command = conn.CreateCommand();
            command.CommandText = commandText;
            command.ExecuteNonQuery();

        }

        public void ApplyFix(List<SqlParameter> parameters)
        {
            if (defaultCounterparty == null)
                throw new ArgumentException("DefaultCounterparty");
            if (paramApReclassActivity == null)
                throw new ArgumentNullException("ApReclassActivity");
            if (payrollFixTag == null)
                throw new ArgumentException("PayrollFixTag");

            var conn = (SqlConnection)objSpace.Session.Connection;
            var command = conn.CreateCommand();
            command.Parameters.AddRange(parameters.ToArray());
            command.CommandText = ProcessCommandText;
            int result = command.ExecuteNonQuery();
        }

        public void Rephase(List<SqlParameter> parameters)
        {

            var conn = (SqlConnection)objSpace.Session.Connection;
            var command = conn.CreateCommand();

            command.Parameters.AddRange(parameters.ToArray());

            command.CommandText = RephaseCommandText;
            command.ExecuteNonQuery();
        }

        public void Reset()
        {
            var conn = (SqlConnection)objSpace.Session.Connection;
            var command = conn.CreateCommand();
            command.CommandText = ResetCommandText;

            var parameters = new List<SqlParameter>()
            {
                new SqlParameter("Snapshot", currentSnapshot.Oid),
            };

            command.Parameters.AddRange(parameters.ToArray());
            command.ExecuteNonQuery();
        }

        private CashFlowSnapshot GetCurrentSnapshot(Session session)
        {
            return CashFlowHelper.GetCurrentSnapshot(session);
        }

        #region Core Sql

        public string ResetCommandText
        {
            get
            {

                return
@"UPDATE CashFlow SET 
IsFixeeSynced = 0,
IsFixerFixeesSynced = 0,
IsFixerSynced = 0
WHERE GCRecord IS NULL
AND [Snapshot] = @Snapshot
";
            }
        }

        public string ProcessCommandText
        {
            get
            {
                return
                    @"-- CashFlowsToFix

IF OBJECT_ID('temp_CashFlowsToFix') IS NOT NULL DROP TABLE temp_CashFlowsToFix;

SELECT cf.* INTO temp_CashFlowsToFix
FROM CashFlow cf
LEFT JOIN CashForecastFixTag tag ON tag.Oid = cf.Fix
LEFT JOIN CashFlow fixer ON fixer.Oid = cf.Oid
WHERE
    cf.GCRecord IS NULL
    AND tag.GCRecord IS NULL
    AND cf.TranDate BETWEEN @FromDate AND @ToDate
	AND cf.[Snapshot] = @Snapshot
    AND (cf.Fix = NULL OR tag.FixTagType != @IgnoreFixTagType)
    AND 
	(
		cf.IsFixeeSynced=0 OR cf.IsFixerSynced=0 OR NOT cf.IsFixerFixeesSynced=0
		OR fixer.GCRecord IS NOT NULL
	)
;

-- Delete Existing Fixes

UPDATE cf
SET 
GCRecord = CAST(RAND() * 2147483646 + 1 AS INT)
FROM CashFlow cf
WHERE cf.GCRecord IS NULL
AND cf.[Snapshot] = @Snapshot
AND cf.Fix IN (@ReversalFixTag, @RevRecFixTag, @ResRevRecFixTag)
AND 
(
	ParentCashFlow IN (SELECT cf2.Oid FROM temp_CashFlowsToFix cf2)
	OR cf.ParentCashFlow IN (SELECT cf1.Oid FROM CashFlow cf1 WHERE cf1.GCRecord IS NULL)
	OR cf.ParentCashFlow IN (SELECT cfd.Oid FROM CashFlow cfd WHERE cfd.GCRecord IS NOT NULL)
)

-- FixeeFixer

IF OBJECT_ID('temp_FixeeFixer') IS NOT NULL DROP TABLE temp_FixeeFixer;

SELECT Fixee, Fixer
INTO temp_FixeeFixer
FROM
(
	SELECT
		-- RowNum = 1 if fixee currency equals fixer currency, otherwise may be greater than 1
		ROW_NUMBER() OVER(
			PARTITION BY fixee.Oid 
			ORDER BY CASE WHEN fixeeAccount.Currency = fixerAccount.Currency THEN 0 ELSE 1 END
		) AS RowNum,
		fixee.Oid AS Fixee,
		fixer.Oid AS Fixer,
		fixee.TranDate,
		fixee.Account,
		fixee.AccountCcyAmt,
		fixee.FixFromDate,
		fixee.FixToDate
	FROM temp_CashFlowsToFix fixee
	LEFT JOIN Counterparty fixeeCparty ON fixeeCparty.Oid = fixee.Counterparty
	LEFT JOIN Account fixeeAccount ON fixeeAccount.Oid = fixee.Account
	LEFT JOIN temp_CashFlowsToFix fixer
		ON fixee.TranDate BETWEEN fixer.FixFromDate AND fixer.FixToDate
			AND fixee.FixActivity = fixer.FixActivity
			AND fixer.[Status] = @ForecastStatus
			AND fixer.FixRank > fixee.FixRank
			AND 
			(
				fixer.Counterparty IS NULL OR fixer.Counterparty = @DefaultCounterparty
				OR fixee.Counterparty IS NULL AND fixer.Counterparty IS NULL
				OR fixer.Counterparty = fixeeCparty.FixCounterparty
				OR fixer.Counterparty = fixee.Counterparty
			)
	LEFT JOIN Account fixerAccount 
		ON fixerAccount.Oid = fixer.Account
	WHERE
		fixee.Account IS NOT NULL AND 
		(
			fixeeAccount.FixAccount = fixerAccount.FixAccount
			OR fixee.Account = fixer.Account
		)
) T1
WHERE RowNum = 1

-- Insert Cash Flow Reversal
IF OBJECT_ID('temp_FixReversal') IS NOT NULL DROP TABLE temp_FixReversal;

SELECT cf.*
INTO temp_FixReversal
FROM temp_CashFlowsToFix cf
JOIN temp_FixeeFixer fixeeFixer ON fixeeFixer.Fixee = cf.Oid;

UPDATE revFix SET 
	Oid = NEWID(),
	ParentCashFlow = revFix.Oid,
	TranDate = fixer.TranDate,

	-- if the currencies do not match, use functional currency
	CounterCcy = CASE WHEN revFix.CounterCcy <> fixer.CounterCcy THEN @FunctionalCurrency ELSE revFix.CounterCcy END,
	CounterCcyAmt = CASE WHEN revFix.CounterCcy <> fixer.CounterCcy THEN -revFix.FunctionalCcyAmt ELSE -revFix.CounterCcyAmt END,

	AccountCcyAmt = -revFix.AccountCcyAmt,
	FunctionalCcyAmt = -revFix.FunctionalCcyAmt,
	Fix = @ReversalFixTag,
	Fixer = NULL

FROM temp_FixReversal revFix
LEFT JOIN temp_FixeeFixer fixeeFixer
	ON fixeeFixer.Fixee = revFix.Oid
LEFT JOIN temp_CashFlowsToFix fixer
	ON fixer.Oid = fixeeFixer.Fixer

-- Link Fixee Cash Flow to Fixer

UPDATE CashFlow
SET Fixer = fixeeFixer.Fixer
FROM CashFlow cf
	INNER JOIN temp_FixeeFixer fixeeFixer ON cf.Oid = fixeeFixer.Fixee

-- Reversal logic for AP Lockdown (i.e. payroll is excluded)

	-- Fixee.RR: Reclass Fixee Fix into AP Pymt
	-- (i.e. reverse the reversal so net reversal is zero because the total amount of AP is correct)
IF OBJECT_ID('temp_FixRevReclass_Fixee') IS NOT NULL DROP TABLE temp_FixRevReclass_Fixee;

SELECT fixee.*
INTO temp_FixRevReclass_Fixee
FROM CashFlow fixee
JOIN temp_FixReversal fr ON fr.ParentCashFlow = fixee.OID
JOIN CashFlow fixer ON fixer.Oid = fixee.Fixer
JOIN CashForecastFixTag fixerTag ON fixerTag.Oid = fixer.Fix
WHERE fixee.TranDate <= @ApayableLockdownDate 
	AND fixer.TranDate <= @ApayableLockdownDate 
	AND (@PayrollFixTag IS NULL OR fixee.Fix != @PayrollFixTag)
	AND fixerTag.FixTagType = @AllocateFixTagType

UPDATE frr SET 
ParentCashFlow = frr.Oid,
Oid = NEWID(),
Fix = @RevRecFixTag,
Activity = @ApReclassActivity,
TranDate = fixer.TranDate
FROM temp_FixRevReclass_Fixee frr
LEFT JOIN CashFlow fixer ON fixer.Oid = frr.Fixer
;

	-- Fixee.RRR: Restore Fixee Fix to future date

IF OBJECT_ID('temp_FixResRevRec_Fixee') IS NOT NULL DROP TABLE temp_FixResRevRec_Fixee;

SELECT fixee.*
INTO temp_FixResRevRec_Fixee
FROM CashFlow fixee
JOIN temp_FixReversal fr ON fr.ParentCashFlow = fixee.OID
JOIN CashFlow fixer ON fixer.Oid = fixee.Fixer
JOIN CashForecastFixTag fixerTag ON fixerTag.Oid = fixer.Fix
WHERE fixee.TranDate <= @ApayableLockdownDate 
	AND fixer.TranDate <= @ApayableLockdownDate 
	AND (@PayrollFixTag IS NULL OR fixee.Fix != @PayrollFixTag)
	AND fixerTag.FixTagType = @AllocateFixTagType

UPDATE frrr SET 
ParentCashFlow = Oid,
Oid = NEWID(),
Fix = @ResRevRecFixTag,
Activity = @ApReclassActivity,
AccountCcyAmt = -frrr.AccountCcyAmt,
CounterCcyAmt = -frrr.CounterCcyAmt,
FunctionalCcyAmt = -frrr.FunctionalCcyAmt,
TranDate = @ApayableNextLockdownDate
FROM temp_FixResRevRec_Fixee frrr
;

	-- Fixer.RR: Reverse Fixer Fix into AP Pymt
IF OBJECT_ID('temp_FixRevReclass_Fixer') IS NOT NULL DROP TABLE temp_FixRevReclass_Fixer;

SELECT fixer.*
INTO temp_FixRevReclass_Fixer
FROM CashFlow fixee
JOIN temp_FixReversal fr ON fr.ParentCashFlow = fixee.OID
JOIN CashFlow fixer ON fixer.Oid = fixee.Fixer
JOIN CashForecastFixTag fixerTag ON fixerTag.Oid = fixer.Fix
WHERE fixee.TranDate <= @ApayableLockdownDate 
	AND fixer.TranDate <= @ApayableLockdownDate 
	AND (@PayrollFixTag IS NULL OR fixee.Fix != @PayrollFixTag)
	AND fixerTag.FixTagType = @AllocateFixTagType

UPDATE frr SET
ParentCashFlow = Oid, 
Oid = NEWID(),
Fix = @RevRecFixTag,
Activity = @ApReclassActivity,
AccountCcyAmt = -AccountCcyAmt,
FunctionalCcyAmt = -FunctionalCcyAmt,
CounterCcyAmt = -CounterCcyAmt,
IsReclass = 1
FROM temp_FixRevReclass_Fixer frr
;

	-- Fixer.RRR: Restore Fixer Fix to future date

IF OBJECT_ID('temp_FixResRevReclass_Fixer') IS NOT NULL DROP TABLE temp_FixResRevReclass_Fixer;

SELECT fixer.*
INTO temp_FixResRevReclass_Fixer
FROM CashFlow fixee
JOIN temp_FixReversal fr ON fr.ParentCashFlow = fixee.OID
JOIN CashFlow fixer ON fixer.Oid = fixee.Fixer
JOIN CashForecastFixTag fixerTag ON fixerTag.Oid = fixer.Fix
WHERE fixee.TranDate <= @ApayableLockdownDate 
	AND fixer.TranDate <= @ApayableLockdownDate 
	AND (@PayrollFixTag IS NULL OR fixee.Fix != @PayrollFixTag)
	AND fixerTag.FixTagType = @AllocateFixTagType

UPDATE frrr SET
ParentCashFlow = Oid, 
Oid = NEWID(),
Fix = @ResRevRecFixTag,
Activity = @ApReclassActivity,
TranDate = @ApayableNextLockdownDate
FROM temp_FixResRevReclass_Fixer frrr
;

-- Finalize

INSERT INTO CashFlow
SELECT * FROM temp_FixReversal
UNION ALL
SELECT * FROM temp_FixRevReclass_Fixee
UNION ALL
SELECT * FROM temp_FixRevReclass_Fixer
UNION ALL
SELECT * FROM temp_FixResRevRec_Fixee
UNION ALL
SELECT * FROM temp_FixResRevReclass_Fixer

-- SELECT
/*
SELECT fixeeFixer.*, 
	fixee.TranDate, 
	fixee.Counterparty,
	fixee.AccountCcyAmt,
	fixee.Account,
	fixeeAccount.FixAccount,
	fixee.FixFromDate,
	fixee.FixToDate
FROM temp_FixeeFixer fixeeFixer
LEFT JOIN CashFlow fixee ON fixee.Oid = fixeeFixer.Fixee
LEFT JOIN Account fixeeAccount ON fixee.Account = fixeeAccount.Oid

SELECT * FROM temp_FixReversal
*/
";
            }
        }

        public string RephaseCommandText
        {
            get
            {
                return
@"DECLARE @MaxActualDate date = (
    SELECT Max(TranDate) FROM CashFlow
    WHERE CashFlow.[Snapshot] = @Snapshot
    AND CashFlow.Status != @ForecastStatus
);

UPDATE cf SET TranDate = CASE
WHEN FixTag.FixTagType = @ScheduleOutFixTagType
	AND cf.FixRank > 2 AND cf.TranDate <= @ApayableLockdownDate
THEN @ApayableNextLockdownDate 
ELSE cf.TranDate
END
FROM CashFlow cf
LEFT JOIN CashForecastFixTag FixTag ON FixTag.Oid = cf.Fix
WHERE cf.[Status] = @ForecastStatus
AND cf.[Snapshot] = @Snapshot

UPDATE CashFlow SET TranDate = DATEADD(d, 1, @MaxActualDate)
FROM CashFlow cf
WHERE cf.[Status] = @ForecastStatus
AND cf.[Snapshot] = @Snapshot
AND cf.TranDate <= @MaxActualDate
";
            }
        }

        #endregion
    }
}
