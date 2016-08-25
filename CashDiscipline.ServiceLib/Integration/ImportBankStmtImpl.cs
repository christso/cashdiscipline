﻿using Microsoft.SqlServer.Management.IntegrationServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashDiscipline.ServiceLib.Integration
{
    public class ImportBankStmtImpl
    {
        private const string SqlConnectionString = CashDiscipline.Common.Constants.SqlConnectionString;
        private const string catalogName = CashDiscipline.Common.Constants.SqlDatabase;
        private const string ssisFolderName = CashDiscipline.Common.Constants.SsisFolderName;
        private const string pkgName = "BankStmt.dtsx";

        public ImportBankStmtImpl()
        {
            SSISMessagesList = new List<string>();
        }

        //string AnzFilePath, bool AnzEnabled,
        //    string WbcFilePath, bool WbcEnabled,
        //    string CbaOpFilePath, bool CbaOpEnabled,
        //    string CbaBosFilePath, bool CbaBosEnabled
        public List<string> SSISMessagesList;

        public void Execute(CashDiscipline.ServiceLib.Types.ImportBankStmtServiceParam paramObj)
        {
            SSISMessagesList.Clear();

            SqlConnection ssisConnection = new SqlConnection(SqlConnectionString);
            IntegrationServices ssisServer = new IntegrationServices(ssisConnection);

            // The reference to the package which you want to execute
            PackageInfo ssisPackage = ssisServer.Catalogs[CashDiscipline.Common.Constants.SsisCatalog].Folders[ssisFolderName].Projects[catalogName].Packages[pkgName];

            // Add execution parameter to override the default asynchronized execution. If you leave this out the package is executed asynchronized
            Collection<PackageInfo.ExecutionValueParameterSet> executionParameter = new Collection<PackageInfo.ExecutionValueParameterSet>();
            executionParameter.Add(new PackageInfo.ExecutionValueParameterSet { ObjectType = 50, ParameterName = "SYNCHRONIZED", ParameterValue = 1 });

            // Modify package parameter
            ssisPackage.Parameters["AnzSourceConnectionString"].Set(ParameterInfo.ParameterValueType.Literal, paramObj.AnzFilePath ?? "");
            ssisPackage.Parameters["AnzDisabled"].Set(ParameterInfo.ParameterValueType.Literal, !paramObj.AnzEnabled);
            ssisPackage.Parameters["WbcSourceConnectionString"].Set(ParameterInfo.ParameterValueType.Literal, paramObj.WbcFilePath ?? "");
            ssisPackage.Parameters["WbcDisabled"].Set(ParameterInfo.ParameterValueType.Literal, !paramObj.WbcEnabled);
            ssisPackage.Parameters["CbaOpSourceConnectionString"].Set(ParameterInfo.ParameterValueType.Literal, paramObj.CbaOpFilePath ?? "");
            ssisPackage.Parameters["CbaOpDisabled"].Set(ParameterInfo.ParameterValueType.Literal, !paramObj.CbaOpEnabled);
            ssisPackage.Parameters["CbaBosSourceConnectionString"].Set(ParameterInfo.ParameterValueType.Literal, paramObj.CbaBosFilePath ?? "");
            ssisPackage.Parameters["CbaBosDisabled"].Set(ParameterInfo.ParameterValueType.Literal, !paramObj.CbaBosEnabled);
            ssisPackage.Alter();

            // Get the identifier of the execution to get the log
            long executionIdentifier = ssisPackage.Execute(false, null, executionParameter);

            // Loop through the log and add the messages to the listbox
            foreach (OperationMessage message in ssisServer.Catalogs["SSISDB"].Executions[executionIdentifier].Messages)
            {
                SSISMessagesList.Add(message.MessageType.ToString() + ": " + message.Message);
            }
        }

        public string GetMessageText()
        {
            string messagesText = string.Empty;
            foreach (var message in SSISMessagesList)
            {
                if (messagesText != string.Empty)
                    messagesText += "\r\n";
                messagesText += message;
            }
            messagesText = messagesText.Replace("\r\n\r\n", "\r\n");
            return messagesText;
        }
    }
}
