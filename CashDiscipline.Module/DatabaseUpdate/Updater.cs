using CashDiscipline.Module.BusinessObjects;
using CashDiscipline.Module.BusinessObjects.Cash;
using CashDiscipline.Module.BusinessObjects.FinAccounting;
using CashDiscipline.Module.ParamObjects.FinAccounting;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.Strategy;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using System;
using System.Data.SqlClient;
using Cash = CashDiscipline.Module.BusinessObjects.Cash;


namespace CashDiscipline.Module.DatabaseUpdate
{
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion)
            : base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();
            CreateFunctions((XPObjectSpace)ObjectSpace);
        }

        protected new void DropColumn(string tableName, string columnName)
        {
            this.ExecuteNonQueryCommand("ALTER TABLE " + tableName + " DROP COLUMN [" + columnName + "]", true);
        }
        protected new void DropConstraint(string tableName, string constraintName)
        {
            Tracing.Tracer.LogText("DropConstraint {0}, {1}", new object[] { tableName, constraintName });
            this.ExecuteNonQueryCommand("alter table " + tableName + " drop constraint " + constraintName, true);
        }

        public static void CreateFunctions(XPObjectSpace os)
        {
            var conn = os.Session.DataLayer.Connection as SqlConnection;
            if (conn == null) return;

            var command = conn.CreateCommand();
            command.CommandText = @"IF OBJECT_ID('dbo.BOMONTH') IS NOT NULL DROP FUNCTION dbo.BOMONTH";
            command.ExecuteNonQuery();

            command.CommandText =
@"CREATE FUNCTION dbo.BOMONTH ( @TranDate date )
RETURNS date
AS
BEGIN
	RETURN DATEADD(d, 1, EOMONTH(@TranDate,-1))
END";
            command.ExecuteNonQuery();
        }

        // Set up the minimum objects for this application to function correctly
        public static void SetupObjects(IObjectSpace objSpace)
        {
            CreateCurrencies(objSpace); // prerequisite for the below
            InitSetOfBooks(objSpace);
            CreateFinAccountingDefaults(objSpace);
            CreateCashFlowDefaults(objSpace);
            Xafology.ExpressApp.StaticHelpers.GetInstance<CashDiscipline.Module.ParamObjects.Cash.CashFlowFixParam>(objSpace);

        }

        public static void CreateFinAccountingDefaults(IObjectSpace objSpace)
        {
            Xafology.ExpressApp.StaticHelpers.GetInstance<FinGenJournalParam>(objSpace);
            Xafology.ExpressApp.StaticHelpers.GetInstance<FinAccountingDefaults>(objSpace);
        }

        public static void InitFixTags(IObjectSpace objSpace)
        {
            var reversalFixTag = objSpace.CreateObject<CashForecastFixTag>();
            reversalFixTag.Name = CashDiscipline.Module.Constants.ReversalFixTag;
            reversalFixTag.FixTagType = CashForecastFixTagType.Ignore;

            var revRecFixTag = objSpace.CreateObject<CashForecastFixTag>();
            revRecFixTag.Name = CashDiscipline.Module.Constants.RevRecFixTag;
            revRecFixTag.FixTagType = CashForecastFixTagType.Ignore;

            var resRevRecFixTag = objSpace.CreateObject<CashForecastFixTag>();
            resRevRecFixTag.Name = CashDiscipline.Module.Constants.ResRevRecFixTag;
            resRevRecFixTag.FixTagType = CashForecastFixTagType.Ignore;

            var payrollFixTag = objSpace.CreateObject<CashForecastFixTag>();
            payrollFixTag.Name = CashDiscipline.Module.Constants.PayrollFixTag;
            payrollFixTag.FixTagType = CashForecastFixTagType.ScheduleOut;

        }

        public static void InitSetOfBooks(IObjectSpace objSpace)
        {
            var setOfBooks = SetOfBooks.GetInstance(objSpace);

            if (setOfBooks.ForexSettleActivity == null)
            {
                var activity = objSpace.FindObject<Activity>(CriteriaOperator.Parse("Name = ?", "Transfer"));
                if (activity == null)
                {
                    activity = objSpace.CreateObject<Activity>();
                    activity.Name = "Transfer";
                    activity.Save();
                }
                setOfBooks.ForexSettleActivity = activity;
            }

            // requires CreateCurrencies
            setOfBooks.FunctionalCurrency = objSpace.FindObject<Currency>(
               CriteriaOperator.Parse(Currency.FieldNames.Name + " = ?", "AUD"));

            if (setOfBooks.ForexSettleCashFlowSource == null)
            {
                var source = objSpace.FindObject<CashFlowSource>(CriteriaOperator.Parse("Name = ?", "Fx Trade"));
                if (source == null)
                {
                    source = objSpace.CreateObject<CashFlowSource>();
                    source.Name = "Fx Trade";
                    source.Save();
                }
                setOfBooks.ForexSettleCashFlowSource = source;
            }
            if (setOfBooks.BankStmtCashFlowSource == null)
            {
                var source = objSpace.FindObject<CashFlowSource>(CriteriaOperator.Parse("Name = ?", "Stmt"));
                if (source == null)
                {
                    source = objSpace.CreateObject<CashFlowSource>();
                    source.Name = "Stmt";
                    source.Save();
                }
                setOfBooks.BankStmtCashFlowSource = source;
            }
            if (setOfBooks.CurrentCashFlowSnapshot == null)
            {
                var snapshot = objSpace.CreateObject<CashFlowSnapshot>();
                snapshot.Name = "Current";
                snapshot.Save();
                setOfBooks.CurrentCashFlowSnapshot = snapshot;
            }

            objSpace.CommitChanges();
        }

        public static void CreateCurrencies(IObjectSpace objSpace)
        {
            var ccy = objSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "AUD"));
            if (ccy == null)
            {
                ccy = objSpace.CreateObject<Currency>();
                ccy.Name = "AUD";
                ccy.Save();
            }
            ccy = objSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "USD"));
            if (ccy == null)
            {
                ccy = objSpace.CreateObject<Currency>();
                ccy.Name = "USD";
                ccy.Save();
            }
        }

        public static void CreateCashFlowDefaults(IObjectSpace objSpace)
        {
            var counterparty = objSpace.FindObject<Counterparty>(CriteriaOperator.Parse("Name = ?", "UNDEFINED"));
            if (counterparty == null)
            {
                counterparty = objSpace.CreateObject<Counterparty>();
                counterparty.Name = "UNDEFINED";
            }
            var activity = objSpace.FindObject<Activity>(CriteriaOperator.Parse("Name = ?", "UNDEFINED"));
            if (activity == null)
            {
                activity = objSpace.CreateObject<Activity>();
                activity.Name = "UNDEFINED";
            }
            var account = objSpace.FindObject<Account>(CriteriaOperator.Parse("Name = ?", "UNDEFINED"));
            if (account == null)
            {
                account = objSpace.CreateObject<Account>();
                account.Name = "UNDEFINED";
                account.Currency = objSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "AUD"));
            }

            var cfDef = Xafology.ExpressApp.StaticHelpers.GetInstance<CashFlowDefaults>(objSpace);
            if (cfDef.Counterparty == null)
                cfDef.Counterparty = counterparty;
            if (cfDef.Activity == null)
                cfDef.Activity = activity;
            if (cfDef.Account == null)
                cfDef.Account = account;
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            SetupObjects(ObjectSpace);
            //SetupSecurity();
        }

        private void SetupSecurity()
        {
            // Administrative role
            SecuritySystemRole adminRole = ObjectSpace.FindObject<SecuritySystemRole>(
               new BinaryOperator("Name", SecurityStrategy.AdministratorRoleName));
            if (adminRole == null)
            {
                adminRole = ObjectSpace.CreateObject<SecuritySystemRole>();
                adminRole.Name = SecurityStrategy.AdministratorRoleName;
                adminRole.IsAdministrative = true;
            }
            // Administrator users
            SecuritySystemUser adminUser;
            adminUser = ObjectSpace.FindObject<SecuritySystemUser>(
                new BinaryOperator("UserName", "admin"));

            if (adminUser == null)
            {
                adminUser = ObjectSpace.CreateObject<SecuritySystemUser>();
                adminUser.UserName = "admin";
                adminUser.Roles.Add(adminRole);
            }

            // Accounts Receivable role
            SecuritySystemRole arRole = ObjectSpace.FindObject<SecuritySystemRole>(
                new BinaryOperator("Name", "Accounts Receivable"));

            if (arRole == null)
            {
                arRole = ObjectSpace.CreateObject<SecuritySystemRole>();
                arRole.Name = "Accounts Receivable";
                arRole.IsAdministrative = false;
            }

            // Accounts Receivable Permissions

            // BankStmt
            GrantFullAcccess(typeof(Cash.BankStmt), arRole);
        }

        // Grant full access if permission object not found for the owner
        // note that if you want to grant full access, you need to delete the existing permissions object
        private void GrantFullAcccess(Type targetType, SecuritySystemRole role)
        {
            var opArArtfRecon = CriteriaOperator.Parse("TargetType = ? And Owner = ?",
                targetType, role);
            SecuritySystemTypePermissionObject typePermission = ObjectSpace.FindObject<SecuritySystemTypePermissionObject>(opArArtfRecon);
            if (typePermission == null)
            {
                typePermission = ObjectSpace.CreateObject<SecuritySystemTypePermissionObject>();
                typePermission.TargetType = targetType;
                typePermission.AllowCreate = true;
                typePermission.AllowDelete = true;
                typePermission.AllowNavigate = true;
                typePermission.AllowRead = true;
                typePermission.AllowWrite = true;
                role.TypePermissions.Add(typePermission);
            }
        }
    }
}
