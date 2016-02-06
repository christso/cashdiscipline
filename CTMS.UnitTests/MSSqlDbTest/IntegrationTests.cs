﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTMS.Module.BusinessObjects.Forex;
using CTMS.Module.BusinessObjects;
using DevExpress.Persistent.Validation;
using System.Diagnostics;
using CTMS.Module.BusinessObjects.Cash;
using DevExpress.Data.Filtering;
using CTMS.Module.Controllers.Forex;
using DevExpress.ExpressApp;
using Xafology.ExpressApp.Xpo;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using CTMS.Module.Controllers.Cash;
using CTMS.Module.DatabaseUpdate;
using CTMS.UnitTests.Base;

namespace CTMS.UnitTests.MSSqlDbTest
{
    [TestFixture]
    public class IntegrationTests : TestBase
    {
        public IntegrationTests()
        {
            SetTesterDbType(TesterDbType.MsSql);
        }

        public override void SetupObjects()
        {
            CTMS.Module.DatabaseUpdate.Updater.CreateCurrencies(ObjectSpace);
            CTMS.Module.DatabaseUpdate.Updater.InitSetOfBooks(ObjectSpace);
        }

        [Test]
        public void ForexTradeToCashFlowToBankStmt_Integrated_SumAreEqual()
        {
            #region Create Forex objects
            // Currencies
            var ccyAUD = ObjectSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "AUD"));
            var ccyUSD = ObjectSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "USD"));

            // Forex Rates
            var rate = ObjectSpace.CreateObject<ForexRate>();
            rate.ConversionDate = new DateTime(2013, 11, 01);
            rate.FromCurrency = ccyAUD;
            rate.ToCurrency = ccyUSD;
            rate.ConversionRate = 0.9M;
            rate.Save();
            ObjectSpace.CommitChanges();
            #endregion

            #region Create Lookup Objects

            var priAccount = ObjectSpace.CreateObject<Account>();
            priAccount.Name = "VHA ANZ 70086";
            priAccount.Currency = ccyAUD;

            var couAccount = ObjectSpace.CreateObject<Account>();
            couAccount.Name = "VHA ANZ USD";
            couAccount.Currency = ccyUSD;

            var forexActivity = ObjectSpace.GetObjectByKey<Activity>(SetOfBooks.CachedInstance.ForexSettleActivity.Oid);

            var outActivity = ObjectSpace.CreateObject<Activity>();
            outActivity.Name = "AP Pymt";

            var outCounterparty = ObjectSpace.CreateObject<Counterparty>();
            outCounterparty.Name = "UNDEFINED";

            var inCounterparty = ObjectSpace.CreateObject<Counterparty>();
            inCounterparty.Name = "ANZ";

            var forexCounterparty = ObjectSpace.CreateObject<ForexCounterparty>();
            forexCounterparty.Name = "ANZ";
            forexCounterparty.CashFlowCounterparty = inCounterparty;
            #endregion

            #region Create Forex Trades

            var ft1 = ObjectSpace.CreateObject<ForexTrade>();
            ft1.ValueDate = new DateTime(2013, 11, 16);
            ft1.Counterparty = forexCounterparty;
            ft1.PrimarySettleAccount = priAccount;
            ft1.CounterSettleAccount = couAccount;
            ft1.PrimaryCcy = ccyAUD;
            ft1.CounterCcy = ccyUSD;
            ft1.Rate = 0.95M;
            ft1.CounterCcyAmt = 100;
            
            var ft2 = ObjectSpace.CreateObject<ForexTrade>();
            ft2.ValueDate = new DateTime(2013, 11, 30);
            ft2.Counterparty = forexCounterparty;
            ft2.PrimarySettleAccount = priAccount;
            ft2.CounterSettleAccount = couAccount;
            ft2.PrimaryCcy = ccyAUD;
            ft2.CounterCcy = ccyUSD;
            ft2.Rate = 0.99M;
            ft2.CounterCcyAmt = 50;

            var ft3 = ObjectSpace.CreateObject<ForexTrade>();
            ft3.ValueDate = new DateTime(2013, 11, 30);
            ft3.Counterparty = forexCounterparty;
            ft3.PrimarySettleAccount = priAccount;
            ft3.CounterSettleAccount = couAccount;
            ft3.PrimaryCcy = ccyAUD;
            ft3.CounterCcy = ccyUSD;
            ft3.Rate = 0.87M;
            ft3.CounterCcyAmt = 30;

            ObjectSpace.CommitChanges();

            var forexTrades = ObjectSpace.GetObjects<ForexTrade>();
            Assert.AreEqual(3, forexTrades.Count);
            #endregion

            #region Test Cash Flows created by Forex Trades
            var fCashFlows = ObjectSpace.GetObjects<CashFlow>();
            Assert.AreEqual(6, fCashFlows.Count());
            Assert.AreEqual(0, Math.Round(fCashFlows.Sum(x => x.FunctionalCcyAmt), 2));
            #endregion

            #region Create Bank Stmt Forex Trade objects

            var bsCouForex1 = ObjectSpace.CreateObject<BankStmt>();
            bsCouForex1.TranDate = new DateTime(2013, 11, 16);
            bsCouForex1.Account = couAccount;
            bsCouForex1.Activity = forexActivity;
            bsCouForex1.Counterparty = outCounterparty;
            bsCouForex1.TranAmount = 100;
            bsCouForex1.ForexSettleType = CashFlowForexSettleType.In;
            bsCouForex1.SummaryDescription = "bsCouForex1";
            bsCouForex1.Save();

            var bsPriForex1 = ObjectSpace.CreateObject<BankStmt>();
            bsPriForex1.TranDate = new DateTime(2013, 11, 16);
            bsPriForex1.Account = priAccount;
            bsPriForex1.Activity = forexActivity;
            bsPriForex1.Counterparty = outCounterparty;
            bsPriForex1.TranAmount = -100 / ft1.Rate;
            bsPriForex1.ForexSettleType = CashFlowForexSettleType.In;
            bsPriForex1.SummaryDescription = "bsPriForex1";
            bsPriForex1.Save();

            var bsCouForex2 = ObjectSpace.CreateObject<BankStmt>();
            bsCouForex2.TranDate = new DateTime(2013, 11, 30);
            bsCouForex2.Account = couAccount;
            bsCouForex2.Activity = forexActivity;
            bsCouForex2.Counterparty = outCounterparty;
            bsCouForex2.TranAmount = 50;
            bsCouForex2.ForexSettleType = CashFlowForexSettleType.In;
            bsCouForex2.SummaryDescription = "bsCouForex2";
            bsCouForex2.Save();

            var bsPriForex2 = ObjectSpace.CreateObject<BankStmt>();
            bsPriForex2.TranDate = new DateTime(2013, 11, 30);
            bsPriForex2.Account = priAccount;
            bsPriForex2.Activity = forexActivity;
            bsPriForex2.Counterparty = outCounterparty;
            bsPriForex2.TranAmount = -50 / ft2.Rate;
            bsPriForex2.ForexSettleType = CashFlowForexSettleType.In;
            bsPriForex2.SummaryDescription = "bsPriForex2";
            bsPriForex2.Save();

            var bsCouForex3 = ObjectSpace.CreateObject<BankStmt>();
            bsCouForex3.TranDate = new DateTime(2013, 11, 30);
            bsCouForex3.Account = couAccount;
            bsCouForex3.Activity = forexActivity;
            bsCouForex3.Counterparty = outCounterparty;
            bsCouForex3.TranAmount = 30;
            bsCouForex3.ForexSettleType = CashFlowForexSettleType.In;
            bsCouForex3.SummaryDescription = "bsCouForex3";
            bsCouForex3.Save();

            var bsPriForex3 = ObjectSpace.CreateObject<BankStmt>();
            bsPriForex3.TranDate = new DateTime(2013, 11, 30);
            bsPriForex3.Account = priAccount;
            bsPriForex3.Activity = forexActivity;
            bsPriForex3.Counterparty = outCounterparty;
            bsPriForex3.TranAmount = -30 / ft3.Rate;
            bsPriForex3.ForexSettleType = CashFlowForexSettleType.In;
            bsPriForex3.SummaryDescription = "bsPriForex3";
            bsPriForex3.Save();

            #endregion

            #region Create Bank Stmt AP payment objects

            decimal bsOutAmounts = 0;

            var bsOut1 = ObjectSpace.CreateObject<BankStmt>();
            bsOut1.TranDate = new DateTime(2013, 12, 17);
            bsOut1.Account = couAccount;
            bsOut1.Activity = outActivity;
            bsOut1.Counterparty = outCounterparty;
            bsOut1.TranAmount = -110;
            bsOut1.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut1.SummaryDescription = "bsOut1";
            bsOut1.Save();
            bsOutAmounts += bsOut1.TranAmount;

            var bsOut2 = ObjectSpace.CreateObject<BankStmt>();
            bsOut2.TranDate = new DateTime(2013, 12, 17);
            bsOut2.Account = couAccount;
            bsOut2.Activity = outActivity;
            bsOut2.Counterparty = outCounterparty;
            bsOut2.TranAmount = 105;
            bsOut2.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut2.SummaryDescription = "bsOut2";
            bsOut2.Save();
            bsOutAmounts += bsOut2.TranAmount;

            var bsOut3 = ObjectSpace.CreateObject<BankStmt>();
            bsOut3.TranDate = new DateTime(2013, 12, 17);
            bsOut3.Account = couAccount;
            bsOut3.Activity = outActivity;
            bsOut3.Counterparty = outCounterparty;
            bsOut3.TranAmount = -105;
            bsOut3.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut3.SummaryDescription = "bsOut3";
            bsOut3.Save();
            bsOutAmounts += bsOut3.TranAmount;

            var bsOut4 = ObjectSpace.CreateObject<BankStmt>();
            bsOut4.TranDate = new DateTime(2013, 12, 17);
            bsOut4.Account = couAccount;
            bsOut4.Activity = outActivity;
            bsOut4.Counterparty = outCounterparty;
            bsOut4.TranAmount = -30;
            bsOut4.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut4.SummaryDescription = "bsOut4";
            bsOut4.Save();
            bsOutAmounts += bsOut4.TranAmount;

            var bsOut5 = ObjectSpace.CreateObject<BankStmt>();
            bsOut5.TranDate = new DateTime(2013, 12, 17);
            bsOut5.Account = couAccount;
            bsOut5.Activity = outActivity;
            bsOut5.Counterparty = outCounterparty;
            bsOut5.TranAmount = 45;
            bsOut5.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut5.SummaryDescription = "bsOut5";
            bsOut5.Save();
            bsOutAmounts += bsOut5.TranAmount;

            var bsOut6 = ObjectSpace.CreateObject<BankStmt>();
            bsOut6.TranDate = new DateTime(2013, 12, 17);
            bsOut6.Account = couAccount;
            bsOut6.Activity = outActivity;
            bsOut6.Counterparty = outCounterparty;
            bsOut6.TranAmount = -45;
            bsOut6.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut6.SummaryDescription = "bsOut6";
            bsOut6.Save();
            bsOutAmounts += bsOut6.TranAmount;

            var bsOut7 = ObjectSpace.CreateObject<BankStmt>();
            bsOut7.TranDate = new DateTime(2013, 12, 17);
            bsOut7.Account = couAccount;
            bsOut7.Activity = outActivity;
            bsOut7.Counterparty = outCounterparty;
            bsOut7.TranAmount = -25;
            bsOut7.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut7.SummaryDescription = "bsOut7";
            bsOut7.Save();
            bsOutAmounts += bsOut7.TranAmount;

            var bsOut8 = ObjectSpace.CreateObject<BankStmt>();
            bsOut8.TranDate = new DateTime(2013, 12, 17);
            bsOut8.Account = couAccount;
            bsOut8.Activity = outActivity;
            bsOut8.Counterparty = outCounterparty;
            bsOut8.TranAmount = -19;
            bsOut8.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut8.SummaryDescription = "bsOut8";
            bsOut8.Save();
            bsOutAmounts += bsOut8.TranAmount;

            #endregion

            ObjectSpace.CommitChanges();

            #region Test Bank Stmt objects before Auto-reconcile
            var bankStmts = ObjectSpace.GetObjects<BankStmt>();
            Assert.AreEqual(-194.69m, bankStmts.Sum(x => x.FunctionalCcyAmt));
            #endregion


            #region Test Bank Stmt objects after Auto-reconcile
            BankStmtViewController.AutoreconcileForexTrades(bankStmts);

            var bsCffs = ObjectSpace.GetObjects<BankStmtCashFlowForecast>();
            Assert.AreEqual(6, bsCffs.Count);
            Assert.AreEqual(-204.44, bankStmts.Sum(x => x.FunctionalCcyAmt));
            #endregion

            // TODO: test deletion after FIFO as well
            BankStmt.DeleteCashFlows(ObjectSpace, "TranDate Between (?,?) And Status = ?",
                new DateTime(2013, 11, 16), new DateTime(2013, 12, 17),
                CashFlowStatus.Forecast);
            BankStmt.UploadToCashFlow(ObjectSpace, new DateTime(2013, 11, 16), new DateTime(2013, 12, 17));

            ObjectSpace.Refresh();
            var cashFlows = ObjectSpace.GetObjects<CashFlow>();
            bankStmts = ObjectSpace.GetObjects<BankStmt>();

            ForexSettleLinkViewController.ForexLinkFifo(ObjectSpace);

            ObjectSpace.Refresh();
            bankStmts = ObjectSpace.GetObjects<BankStmt>();
            cashFlows = ObjectSpace.GetObjects<CashFlow>();

            var fsls = ObjectSpace.GetObjects<ForexSettleLink>();

            #region Final Tests
            var aCashFlows = cashFlows.Where(x => x.Status == CashFlowStatus.Actual);

            Assert.AreEqual(bankStmts.Sum(x => x.TranAmount), aCashFlows.Sum(x => x.AccountCcyAmt));
            //Assert.AreEqual(bankStmts.Sum(x => x.FunctionalCcyAmt), aCashFlows.Sum(x => x.FunctionalCcyAmt));

            Assert.AreEqual(180, fsls.Sum(x => x.AccountCcyAmt));
            Assert.AreEqual(190.25, Math.Round(fsls.Sum(x => x.FunctionalCcyAmt), 2)); // failed

            Assert.AreEqual(-4.44M, Math.Round(cashFlows.Where(x => x.Status == CashFlowStatus.Actual
                && x.Account.Oid == couAccount.Oid)
                .Sum(x => x.FunctionalCcyAmt), 2));
            Assert.AreEqual(-4.44M, Math.Round(bankStmts.Where(x => x.Account.Oid == couAccount.Oid)
                .Sum(x => x.FunctionalCcyAmt), 2));
            #endregion
        }

        [Test]
        public void BankStmtToCashFlow_Upload_SumAreEqual()
        {
            #region Create Forex objects
            // Currencies
            var ccyAUD = ObjectSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "AUD"));
            var ccyUSD = ObjectSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "USD"));

            // Forex Rates
            var rate = ObjectSpace.CreateObject<ForexRate>();
            rate.ConversionDate = new DateTime(2013, 11, 01);
            rate.FromCurrency = ccyAUD;
            rate.ToCurrency = ccyUSD;
            rate.ConversionRate = 0.9M;
            rate.Save();
            ObjectSpace.CommitChanges();
            #endregion

            #region Create Lookup Objects

            var priAccount = ObjectSpace.CreateObject<Account>();
            priAccount.Name = "VHA ANZ 70086";
            priAccount.Currency = ccyAUD;

            var couAccount = ObjectSpace.CreateObject<Account>();
            couAccount.Name = "VHA ANZ USD";
            couAccount.Currency = ccyUSD;

            var forexActivity = ObjectSpace.GetObjectByKey<Activity>(SetOfBooks.CachedInstance.ForexSettleActivity.Oid);

            var outActivity = ObjectSpace.CreateObject<Activity>();
            outActivity.Name = "AP Pymt";

            var outCounterparty = ObjectSpace.CreateObject<Counterparty>();
            outCounterparty.Name = "UNDEFINED";

            var inCounterparty = ObjectSpace.CreateObject<Counterparty>();
            inCounterparty.Name = "ANZ";

            #endregion

            #region Create Bank Stmt objects

            decimal fRate1 = 0.95M;
            decimal fRate2 = 0.99M;
            decimal fRate3 = 0.87M;

            var bsCouForex1 = ObjectSpace.CreateObject<BankStmt>();
            bsCouForex1.TranDate = new DateTime(2013, 11, 16);
            bsCouForex1.Account = couAccount;
            bsCouForex1.Activity = forexActivity;
            bsCouForex1.Counterparty = outCounterparty;
            bsCouForex1.TranAmount = 100;
            bsCouForex1.ForexSettleType = CashFlowForexSettleType.In;
            bsCouForex1.SummaryDescription = "bsCouForex1";
            bsCouForex1.Save();

            var bsPriForex1 = ObjectSpace.CreateObject<BankStmt>();
            bsPriForex1.TranDate = new DateTime(2013, 11, 16);
            bsPriForex1.Account = priAccount;
            bsPriForex1.Activity = forexActivity;
            bsPriForex1.Counterparty = outCounterparty;
            bsPriForex1.TranAmount = -100 / fRate1;
            bsPriForex1.ForexSettleType = CashFlowForexSettleType.In;
            bsPriForex1.SummaryDescription = "bsPriForex1";
            bsPriForex1.Save();

            var bsCouForex2 = ObjectSpace.CreateObject<BankStmt>();
            bsCouForex2.TranDate = new DateTime(2013, 11, 30);
            bsCouForex2.Account = couAccount;
            bsCouForex2.Activity = forexActivity;
            bsCouForex2.Counterparty = outCounterparty;
            bsCouForex2.TranAmount = 50;
            bsCouForex2.ForexSettleType = CashFlowForexSettleType.In;
            bsCouForex2.SummaryDescription = "bsCouForex2";
            bsCouForex2.Save();

            var bsPriForex2 = ObjectSpace.CreateObject<BankStmt>();
            bsPriForex2.TranDate = new DateTime(2013, 11, 30);
            bsPriForex2.Account = priAccount;
            bsPriForex2.Activity = forexActivity;
            bsPriForex2.Counterparty = outCounterparty;
            bsPriForex2.TranAmount = -50 / fRate2;
            bsPriForex2.ForexSettleType = CashFlowForexSettleType.In;
            bsPriForex2.SummaryDescription = "bsPriForex2";
            bsPriForex2.Save();

            var bsCouForex3 = ObjectSpace.CreateObject<BankStmt>();
            bsCouForex3.TranDate = new DateTime(2013, 11, 30);
            bsCouForex3.Account = couAccount;
            bsCouForex3.Activity = forexActivity;
            bsCouForex3.Counterparty = outCounterparty;
            bsCouForex3.TranAmount = 30;
            bsCouForex3.ForexSettleType = CashFlowForexSettleType.In;
            bsCouForex3.SummaryDescription = "bsCouForex3";
            bsCouForex3.Save();

            var bsPriForex3 = ObjectSpace.CreateObject<BankStmt>();
            bsPriForex3.TranDate = new DateTime(2013, 11, 30);
            bsPriForex3.Account = priAccount;
            bsPriForex3.Activity = forexActivity;
            bsPriForex3.Counterparty = outCounterparty;
            bsPriForex3.TranAmount = -30 / fRate3;
            bsPriForex3.ForexSettleType = CashFlowForexSettleType.In;
            bsPriForex3.SummaryDescription = "bsPriForex3";
            bsPriForex3.Save();

            var bsOut1 = ObjectSpace.CreateObject<BankStmt>();
            bsOut1.TranDate = new DateTime(2013, 12, 17);
            bsOut1.Account = couAccount;
            bsOut1.Activity = outActivity;
            bsOut1.Counterparty = outCounterparty;
            bsOut1.TranAmount = -110;
            bsOut1.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut1.SummaryDescription = "bsOut1";
            bsOut1.Save();

            var bsOut2 = ObjectSpace.CreateObject<BankStmt>();
            bsOut2.TranDate = new DateTime(2013, 12, 17);
            bsOut2.Account = couAccount;
            bsOut2.Activity = outActivity;
            bsOut2.Counterparty = outCounterparty;
            bsOut2.TranAmount = 105;
            bsOut2.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut2.SummaryDescription = "bsOut2";
            bsOut2.Save();

            var bsOut3 = ObjectSpace.CreateObject<BankStmt>();
            bsOut3.TranDate = new DateTime(2013, 12, 17);
            bsOut3.Account = couAccount;
            bsOut3.Activity = outActivity;
            bsOut3.Counterparty = outCounterparty;
            bsOut3.TranAmount = -105;
            bsOut3.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut3.SummaryDescription = "bsOut3";
            bsOut3.Save();

            var bsOut4 = ObjectSpace.CreateObject<BankStmt>();
            bsOut4.TranDate = new DateTime(2013, 12, 17);
            bsOut4.Account = couAccount;
            bsOut4.Activity = outActivity;
            bsOut4.Counterparty = outCounterparty;
            bsOut4.TranAmount = -30;
            bsOut4.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut4.SummaryDescription = "bsOut4";
            bsOut4.Save();

            var bsOut5 = ObjectSpace.CreateObject<BankStmt>();
            bsOut5.TranDate = new DateTime(2013, 12, 17);
            bsOut5.Account = couAccount;
            bsOut5.Activity = outActivity;
            bsOut5.Counterparty = outCounterparty;
            bsOut5.TranAmount = 45;
            bsOut5.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut5.SummaryDescription = "bsOut5";
            bsOut5.Save();

            var bsOut6 = ObjectSpace.CreateObject<BankStmt>();
            bsOut6.TranDate = new DateTime(2013, 12, 17);
            bsOut6.Account = couAccount;
            bsOut6.Activity = outActivity;
            bsOut6.Counterparty = outCounterparty;
            bsOut6.TranAmount = -45;
            bsOut6.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut6.SummaryDescription = "bsOut6";
            bsOut6.Save();

            var bsOut7 = ObjectSpace.CreateObject<BankStmt>();
            bsOut7.TranDate = new DateTime(2013, 12, 17);
            bsOut7.Account = couAccount;
            bsOut7.Activity = outActivity;
            bsOut7.Counterparty = outCounterparty;
            bsOut7.TranAmount = -25;
            bsOut7.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut7.SummaryDescription = "bsOut7";
            bsOut7.Save();

            var bsOut8 = ObjectSpace.CreateObject<BankStmt>();
            bsOut8.TranDate = new DateTime(2013, 12, 17);
            bsOut8.Account = couAccount;
            bsOut8.Activity = outActivity;
            bsOut8.Counterparty = outCounterparty;
            bsOut8.TranAmount = -19;
            bsOut8.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut8.SummaryDescription = "bsOut8";
            bsOut8.Save();
            #endregion

            ObjectSpace.CommitChanges();

            BankStmt.UploadToCashFlow(ObjectSpace, new DateTime(2013, 11, 16), new DateTime(2013, 12, 17));

            ObjectSpace.Refresh();

            var bankStmts = ObjectSpace.GetObjects<BankStmt>();
            var cashFlows = ObjectSpace.GetObjects<CashFlow>();

            Assert.AreEqual(bankStmts.Sum(x => x.TranAmount), cashFlows.Sum(x => x.AccountCcyAmt));
        }

        [Test]
        public void CashFlow_ForexLinkFifo_AmountsAreEqual()
        {
            var ccyUSD = ObjectSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "USD"));

            var account = ObjectSpace.CreateObject<Account>();
            account.Name = "VHA ANZ USD";
            account.Currency = ccyUSD;

            var cfIn1 = ObjectSpace.CreateObject<CashFlow>();
            cfIn1.TranDate = new DateTime(2013, 11, 16);
            cfIn1.Account = account;
            cfIn1.AccountCcyAmt = 100;
            cfIn1.ForexSettleType = CashFlowForexSettleType.In;
            cfIn1.Save();

            var cfIn2 = ObjectSpace.CreateObject<CashFlow>();
            cfIn2.TranDate = new DateTime(2013, 11, 30);
            cfIn2.Account = account;
            cfIn2.AccountCcyAmt = 50;
            cfIn2.ForexSettleType = CashFlowForexSettleType.In;
            cfIn2.Save();

            var cfOut1 = ObjectSpace.CreateObject<CashFlow>();
            cfOut1.TranDate = new DateTime(2013, 12, 17);
            cfOut1.Account = account;
            cfOut1.AccountCcyAmt = -90;
            cfOut1.ForexSettleType = CashFlowForexSettleType.Out;
            cfOut1.Save();

            var cfOut2 = ObjectSpace.CreateObject<CashFlow>();
            cfOut2.TranDate = new DateTime(2013, 12, 17);
            cfOut2.Account = account;
            cfOut2.AccountCcyAmt = 20;
            cfOut2.ForexSettleType = CashFlowForexSettleType.Out;
            cfOut2.Save();

            var cfOut3 = ObjectSpace.CreateObject<CashFlow>();
            cfOut3.TranDate = new DateTime(2013, 12, 17);
            cfOut3.Account = account;
            cfOut3.AccountCcyAmt = -20;
            cfOut3.ForexSettleType = CashFlowForexSettleType.Out;
            cfOut3.Save();

            var cfOut4 = ObjectSpace.CreateObject<CashFlow>();
            cfOut4.TranDate = new DateTime(2013, 12, 17);
            cfOut4.Account = account;
            cfOut4.AccountCcyAmt = -40;
            cfOut4.ForexSettleType = CashFlowForexSettleType.Out;
            cfOut4.Save();

            var cfOut6 = ObjectSpace.CreateObject<CashFlow>();
            cfOut6.TranDate = new DateTime(2013, 12, 17);
            cfOut6.Account = account;
            cfOut6.AccountCcyAmt = -30;
            cfOut6.ForexSettleType = CashFlowForexSettleType.Out;
            cfOut6.Save();

            ObjectSpace.CommitChanges();

            ForexSettleLinkViewController.ForexLinkFifo(ObjectSpace, 20);

            var fsls = ObjectSpace.GetObjects<ForexSettleLink>();

            Assert.AreEqual(6, fsls.Count());
            Assert.AreEqual(150, fsls.Sum(x => x.AccountCcyAmt));
        }

        [Test]
        public void BankStmt_UploadToCashFlowAndForexSettleLinkFifo_SumAreEqual()
        {
            // Currencies
            var ccyAUD = ObjectSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "AUD"));
            var ccyUSD = ObjectSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "USD"));

            // Forex Rates
            var rate = ObjectSpace.CreateObject<ForexRate>();
            rate.ConversionDate = new DateTime(2013, 11, 01);
            rate.FromCurrency = ccyAUD;
            rate.ToCurrency = ccyUSD;
            rate.ConversionRate = 0.9M;
            rate.Save();
            ObjectSpace.CommitChanges();

            var account = ObjectSpace.CreateObject<Account>();
            account.Name = "VHA ANZ USD";
            account.Currency = ccyUSD;

            var activity = ObjectSpace.CreateObject<Activity>();
            activity.Name = "AP Pymt";

            var counterparty = ObjectSpace.CreateObject<Counterparty>();
            counterparty.Name = "SAMSUNG";

            var bsIn1 = ObjectSpace.CreateObject<BankStmt>();
            bsIn1.TranDate = new DateTime(2013, 11, 16);
            bsIn1.Account = account;
            bsIn1.Activity = activity;
            bsIn1.Counterparty = counterparty;
            bsIn1.TranAmount = 100;
            bsIn1.FunctionalCcyAmt = bsIn1.TranAmount / 0.95M;
            bsIn1.ForexSettleType = CashFlowForexSettleType.In;
            bsIn1.SummaryDescription = "bsIn1";
            bsIn1.Save();

            var bsIn2 = ObjectSpace.CreateObject<BankStmt>();
            bsIn2.TranDate = new DateTime(2013, 11, 30);
            bsIn2.Account = account;
            bsIn2.Activity = activity;
            bsIn2.Counterparty = counterparty;
            bsIn2.TranAmount = 50;
            bsIn2.FunctionalCcyAmt = bsIn2.TranAmount / 0.99M;
            bsIn2.ForexSettleType = CashFlowForexSettleType.In;
            bsIn2.SummaryDescription = "bsIn2";
            bsIn2.Save();

            var bsIn3 = ObjectSpace.CreateObject<BankStmt>();
            bsIn3.TranDate = new DateTime(2013, 11, 30);
            bsIn3.Account = account;
            bsIn3.Activity = activity;
            bsIn3.Counterparty = counterparty;
            bsIn3.TranAmount = 30;
            bsIn3.FunctionalCcyAmt = bsIn3.TranAmount / 0.87M;
            bsIn3.ForexSettleType = CashFlowForexSettleType.In;
            bsIn3.SummaryDescription = "bsIn3";
            bsIn3.Save();

            var bsOut1 = ObjectSpace.CreateObject<BankStmt>();
            bsOut1.TranDate = new DateTime(2013, 12, 17);
            bsOut1.Account = account;
            bsOut1.Activity = activity;
            bsOut1.Counterparty = counterparty;
            bsOut1.TranAmount = -110;
            bsOut1.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut1.Save();

            var bsOut2 = ObjectSpace.CreateObject<BankStmt>();
            bsOut2.TranDate = new DateTime(2013, 12, 17);
            bsOut2.Account = account;
            bsOut2.Activity = activity;
            bsOut2.Counterparty = counterparty;
            bsOut2.TranAmount = 105;
            bsOut2.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut2.SummaryDescription = "bsOut2";
            bsOut2.Save();

            var bsOut3 = ObjectSpace.CreateObject<BankStmt>();
            bsOut3.TranDate = new DateTime(2013, 12, 17);
            bsOut3.Account = account;
            bsOut3.Activity = activity;
            bsOut3.Counterparty = counterparty;
            bsOut3.TranAmount = -105;
            bsOut3.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut3.SummaryDescription = "bsOut3";
            bsOut3.Save();

            var bsOut4 = ObjectSpace.CreateObject<BankStmt>();
            bsOut4.TranDate = new DateTime(2013, 12, 17);
            bsOut4.Account = account;
            bsOut4.Activity = activity;
            bsOut4.Counterparty = counterparty;
            bsOut4.TranAmount = -30;
            bsOut4.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut4.SummaryDescription = "bsOut4";
            bsOut4.Save();

            var bsOut5 = ObjectSpace.CreateObject<BankStmt>();
            bsOut5.TranDate = new DateTime(2013, 12, 17);
            bsOut5.Account = account;
            bsOut5.Activity = activity;
            bsOut5.Counterparty = counterparty;
            bsOut5.TranAmount = 45;
            bsOut5.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut5.SummaryDescription = "bsOut5";
            bsOut5.Save();

            var bsOut6 = ObjectSpace.CreateObject<BankStmt>();
            bsOut6.TranDate = new DateTime(2013, 12, 17);
            bsOut6.Account = account;
            bsOut6.Activity = activity;
            bsOut6.Counterparty = counterparty;
            bsOut6.TranAmount = -45;
            bsOut6.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut6.SummaryDescription = "bsOut6";
            bsOut6.Save();

            var bsOut7 = ObjectSpace.CreateObject<BankStmt>();
            bsOut7.TranDate = new DateTime(2013, 12, 17);
            bsOut7.Account = account;
            bsOut7.Activity = activity;
            bsOut7.Counterparty = counterparty;
            bsOut7.TranAmount = -25;
            bsOut7.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut7.SummaryDescription = "bsOut7";
            bsOut7.Save();

            var bsOut8 = ObjectSpace.CreateObject<BankStmt>();
            bsOut8.TranDate = new DateTime(2013, 12, 17);
            bsOut8.Account = account;
            bsOut8.Activity = activity;
            bsOut8.Counterparty = counterparty;
            bsOut8.TranAmount = -19;
            bsOut8.ForexSettleType = CashFlowForexSettleType.Out;
            bsOut8.SummaryDescription = "bsOut8";
            bsOut8.Save();

            ObjectSpace.CommitChanges();

            var objSpace = ObjectSpace;

            BankStmt.UploadToCashFlow(objSpace, new DateTime(2013, 11, 16), new DateTime(2013, 12, 17));

            ForexSettleLinkViewController.ForexLinkFifo(objSpace);

            var fsls = objSpace.GetObjects<ForexSettleLink>();
     
            Assert.AreEqual(14, fsls.Count());
            Assert.AreEqual(180, fsls.Sum(x => x.AccountCcyAmt));
            Assert.AreEqual(190.25, Math.Round(fsls.Sum(x => x.FunctionalCcyAmt), 2));
            var cashFlows = objSpace.GetObjects<CashFlow>();
            var bankStmts = objSpace.GetObjects<BankStmt>();
            Assert.AreEqual(bankStmts.Count, cashFlows.Count);
            Assert.AreEqual(-4.44M, Math.Round(cashFlows.Sum(x => x.FunctionalCcyAmt), 2));
            Assert.AreEqual(-4.44M, Math.Round(bankStmts.Sum(x => x.FunctionalCcyAmt), 2));
        }


        // check total amounts
        [Test]
        public void CashFlow_ForexSettleLinkAsParam_Equals()
        {
            var ccyUSD = ObjectSpace.FindObject<Currency>(CriteriaOperator.Parse("Name = ?", "USD"));

            var account = ObjectSpace.CreateObject<Account>();
            account.Name = "VHA ANZ USD";
            account.Currency = ccyUSD;

            var cfIn1 = ObjectSpace.CreateObject<CashFlow>();
            cfIn1.TranDate = new DateTime(2012, 06, 01);
            cfIn1.Account = account;
            cfIn1.AccountCcyAmt = 2000000;
            cfIn1.Save();

            var cfOut1 = ObjectSpace.CreateObject<CashFlow>();
            cfOut1.TranDate = new DateTime(2012, 07, 01);
            cfOut1.Account = account;
            cfOut1.AccountCcyAmt = -1500000;
            cfOut1.Save();

            var fsl1 = ObjectSpace.CreateObject<ForexSettleLink>();
            fsl1.CashFlowIn = cfIn1;
            fsl1.CashFlowOut = cfOut1;
            fsl1.AccountCcyAmt = 1000000;
            fsl1.Save();

            ObjectSpace.CommitChanges();

            Assert.AreEqual(1000000, cfIn1.ForexLinkedInAccountCcyAmt);
            Assert.AreEqual(-1000000, cfOut1.ForexLinkedOutAccountCcyAmt);

            var fsl2 = ObjectSpace.CreateObject<ForexSettleLink>();
            fsl2.CashFlowIn = cfIn1;
            fsl2.CashFlowOut = cfOut1;
            fsl2.AccountCcyAmt = 500000;
            fsl2.Save();
            ObjectSpace.CommitChanges();

            var forexLinkedAmt = fsl1.AccountCcyAmt + fsl2.AccountCcyAmt;

            Assert.AreEqual(forexLinkedAmt, cfIn1.ForexLinkedInAccountCcyAmt);
            Assert.AreEqual(cfIn1.ForexLinkedInAccountCcyAmt, cfIn1.ForexLinkedAccountCcyAmt);
            Assert.AreEqual(cfIn1.AccountCcyAmt - forexLinkedAmt, cfIn1.ForexUnlinkedAccountCcyAmt);
            Assert.AreEqual(false, cfIn1.ForexLinkIsClosed);

            Assert.AreEqual(-forexLinkedAmt, cfOut1.ForexLinkedOutAccountCcyAmt);
            Assert.AreEqual(cfOut1.ForexLinkedOutAccountCcyAmt, cfOut1.ForexLinkedAccountCcyAmt);
            Assert.AreEqual(cfOut1.AccountCcyAmt + forexLinkedAmt, cfOut1.ForexUnlinkedAccountCcyAmt);
            Assert.AreEqual(true, cfOut1.ForexLinkIsClosed);
        }


    }
}
