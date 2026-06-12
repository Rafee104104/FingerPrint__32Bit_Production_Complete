using System;
using System.Collections.Generic;
#nullable disable
using System.Data;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace FingerPrintForm;

public class Oracle_database_helper
{
    public OracleConnection DBCon;
    private OracleCommand DBCmd;

    // DB DATA
    public OracleDataAdapter DBDA;
    public DataTable DBDT;

    // QUERY PARAMETERS
    public List<OracleParameter> Params = new List<OracleParameter>();

    // QUERY STATISTICS
    public int RecordCount;
    public string Exception;

    public Oracle_database_helper()
    {
    }

    // EXECUTE QUERY METHOD
    public void ExecQuery(string Query, bool ReturnIdentity = false)
    {
        // RESET QUERY STATS
        RecordCount = 0;
        Exception = "";

        try
        {
            DBCon = new OracleConnection(
                "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1522)))(CONNECT_DATA=(SERVICE_NAME=ORCLBS)));User Id=BSOFFICE;Password=bsoffice;"
            );

            DBCon.Open();

            // CREATE DB COMMAND
            DBCmd = new OracleCommand(Query, DBCon);
            DBCmd.BindByName = true;

            // LOAD PARAMS INTO DB COMMAND
            List<OracleParameter> commandParams = new List<OracleParameter>(Params);
            commandParams.ForEach(p => DBCmd.Parameters.Add(p));

            // CLEAR PARAM LIST
            Params.Clear();

            // EXECUTE COMMAND & FILL DATASET
            DBDT = new DataTable();
            DBDA = new OracleDataAdapter(DBCmd);
            RecordCount = DBDA.Fill(DBDT);

            if (ReturnIdentity == true)
            {
                string ReturnQuery =
                    "SELECT USERID || '|' || TO_CHAR(CHECKTIME, 'YYYYMMDDHH24MISS') AS LastID " +
                    "FROM ATT WHERE USERID = :emp_id AND CHECKTIME = :checktime";

                DBCmd = new OracleCommand(ReturnQuery, DBCon);
                DBCmd.BindByName = true;
                DBCmd.Parameters.Add(":emp_id", OracleDbType.Varchar2).Value = GetParamValue(commandParams, ":emp_id");
                DBCmd.Parameters.Add(":checktime", OracleDbType.Date).Value = GetParamValue(commandParams, ":checktime");

                DBDT = new DataTable();
                DBDA = new OracleDataAdapter(DBCmd);
                RecordCount = DBDA.Fill(DBDT);
            }
        }
        catch (System.Exception ex)
        {
            // CAPTURE ERROR
            Exception = "ExecQuery Error: \r\n" + ex.Message;
        }
        finally
        {
            // CLOSE CONNECTION
            if (DBCon != null && DBCon.State == ConnectionState.Open)
            {
                DBCon.Close();
            }
        }
    }

    // ADD PARAMS
    public void AddParam(string Name, object Value)
    {
        OracleParameter NewParam = new OracleParameter(Name, Value);
        Params.Add(NewParam);
    }

    private static object GetParamValue(List<OracleParameter> parameters, string name)
    {
        string normalizedName = name.TrimStart(':');

        foreach (OracleParameter parameter in parameters)
        {
            if (string.Equals(parameter.ParameterName.TrimStart(':'), normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return parameter.Value;
            }
        }

        return DBNull.Value;
    }

    // ERROR CHECKING
    public bool HasException(bool Report = false)
    {
        if (string.IsNullOrEmpty(Exception))
        {
            return false;
        }

        if (Report == true)
        {
            MessageBox.Show(Exception, "Exception:", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return true;
    }
}
