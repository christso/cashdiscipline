﻿using CashDiscipline.Module.ParamObjects.Import;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Xpo;
using Excel;
using LumenWorks.Framework.IO.Csv;
using SmartFormat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace CashDiscipline.Module.Clients
{
    public class SqlServerLoader2 : IDisposable
    {
        public SqlServerLoader2(SqlConnection sqlConn)
        {
            this.sqlConn = sqlConn;
            this.bc = new SqlBulkCopy(sqlConn);
        }

        private readonly SqlConnection sqlConn;
        private readonly SqlBulkCopy bc;

        private string tempTableName;
        public string TempTableName
        {
            get { return this.tempTableName; }
            set { this.tempTableName = value; }
        }

        private string createSql;
        public string CreateSql
        {
            get { return this.createSql; }
            set { this.createSql = value; }
        }

        private string persistSql;
        public string PersistSql
        {
            get { return this.persistSql; }
            set { this.persistSql = value; }
        }

        public string Execute(DataTable sourceTable)
        {
            return Execute((object)sourceTable);
        }

        public string Execute(IDataReader sourceReader)
        {
            return Execute((object)sourceReader);
        }

        private string FormatSql(string commandText)
        {
            return Smart.Format(commandText, new { TempTable = tempTableName });
        }

        public SqlBulkCopyColumnMappingCollection ColumnMappings
        {
            get
            {
                return bc.ColumnMappings;
            }
        }

        private string Execute(object source)
        {
            if (string.IsNullOrEmpty(tempTableName))
                tempTableName = "#tmp_" + Guid.NewGuid().ToString("N");

            int rowCount = 0;
            var statusMessage = string.Empty;

            using (var cmd = sqlConn.CreateCommand())
            {
                cmd.CommandTimeout = CashDiscipline.Common.Constants.SqlCommandTimeout;

                cmd.CommandText = FormatSql(
                    "IF OBJECT_ID('tempdb..{TempTable}') IS NOT NULL DROP TABLE {TempTable}\r\n"
                    + createSql);
                cmd.ExecuteNonQuery();
                
                bc.DestinationTableName = tempTableName;

                #region write source to destination
                if (source is IDataReader)
                    bc.WriteToServer((IDataReader)source);
                else if (source is DataTable)
                    bc.WriteToServer((DataTable)source);
                else
                    throw new InvalidOperationException(string.Format(
                        "Source must be of type IDataReader or DataTable. Type is {0}", source.GetType()));
                #endregion

                cmd.CommandText = FormatSql("SELECT COUNT(*) FROM {TempTable}");
                rowCount = Convert.ToInt32(cmd.ExecuteScalar());

                try
                {
                    cmd.CommandText = FormatSql(persistSql);
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException(string.Format(
                        "SQL Exception: {0} at Line Number {1}-------\r\n{2}", ex.Message, ex.LineNumber, cmd.CommandText));
                }
            }
            statusMessage = string.Format("{0} rows processed.", rowCount);
            return statusMessage;

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (bc != null)
                        ((IDisposable)bc).Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SqlServerLoader2() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}