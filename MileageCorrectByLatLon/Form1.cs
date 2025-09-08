using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MileageCorrectByLatLon
{
    public partial class Form1 : Form
    {
        string conStr = ConfigurationManager.ConnectionStrings["GPSDBConnectionString"].ConnectionString;
        SqlConnection con;
        SqlCommand cmd;
        SqlDataReader rd;

        SqlConnection con1;
        SqlCommand cmd1;

        SqlConnection con2;
        SqlCommand cmd2;
        SqlDataReader rd2;

        public Form1()
        {
            InitializeComponent();            
        }

        private void btnRepair_Click(object sender, EventArgs e)
        {
            con = new SqlConnection(conStr);
            cmd = new SqlCommand();
            cmd.Connection = con;
            con.Open();
            cmd.CommandText = "select TABLE_NAME from INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%Track_%'";
            rd = cmd.ExecuteReader();
            List<INFORMATION_SCHEMA> infoSchemaS = new List<INFORMATION_SCHEMA>();
            while (rd.Read())
            {
                INFORMATION_SCHEMA infoSchema = new INFORMATION_SCHEMA();
                infoSchema.TABLE_NAME = rd.GetString(0);
                infoSchemaS.Add(infoSchema);
            }
            rd.Close();
            con.Close();

            foreach(INFORMATION_SCHEMA infoSchema in infoSchemaS)
            {
                bool isCorrected = false;
                con1 = new SqlConnection(conStr);
                cmd1 = new SqlCommand();
                cmd1.Connection = con1;
                con1.Open();
                cmd1.CommandText = "SELECT [TableName] FROM [All_Track] where TableName = '" + infoSchema.TABLE_NAME + "'";
                rd = cmd1.ExecuteReader();
                rd.Read();
                if (rd.HasRows)
                {
                    isCorrected = true;
                }
                con1.Close();

                if (!isCorrected)
                {
                    con1 = new SqlConnection(conStr);
                    cmd1 = new SqlCommand();
                    cmd1.Connection = con1;
                    con1.Open();
                    cmd1.CommandText = "delete from " + infoSchema.TABLE_NAME + " where dbLat <1 or dbLon <1";
                    cmd1.ExecuteNonQuery();
                    con1.Close();

                    con = new SqlConnection(conStr);
                    cmd = new SqlCommand();
                    cmd.Connection = con;
                    con.Open();
                    cmd.CommandText = "select nTime, dbLon, dbLat, nMileage from " + infoSchema.TABLE_NAME + " WHERE nTime BETWEEN DATEDIFF (SECOND, '1970-01-01 06:00:00', '2017-04-15 00:00:00') AND DATEDIFF(SECOND, '1970-01-01 06:00:00', GETDATE()) ORDER BY nTime";
                    rd = cmd.ExecuteReader();
                    int i = 0;
                    decimal lon = 0;
                    decimal lat = 0;
                    int m = 0;
                    int calMile;
                    int nTime = 0;

                    while (rd.Read())
                    {
                        i++;
                        if (i == 1)
                        {
                            lon = rd.GetDecimal(1);
                            lat = rd.GetDecimal(2);
                            m = rd.GetInt32(3);
                        }
                        else if (i > 1)
                        {
                            calMile = Convert.ToInt32(DirectDistance(Convert.ToDouble(lat), Convert.ToDouble(lon), Convert.ToDouble(rd.GetDecimal(2)), Convert.ToDouble(rd.GetDecimal(1))));
                            if (calMile > 100000)
                            {
                                calMile = 100;
                            }
                            calMile = calMile + m;
                            nTime = rd.GetInt32(0);

                            con1 = new SqlConnection(conStr);
                            cmd1 = new SqlCommand();
                            cmd1.Connection = con1;
                            con1.Open();
                            cmd1.CommandText = "update " + infoSchema.TABLE_NAME + " set nMileage = " + calMile + " where nTime = " + nTime + "";
                            cmd1.ExecuteNonQuery();
                            cmd1.Dispose();
                            con1.Close();

                            lon = rd.GetDecimal(1);
                            lat = rd.GetDecimal(2);
                            m = calMile;
                        }
                    }
                    rd.Close();
                    con.Close();

                    con = new SqlConnection(conStr);
                    cmd = new SqlCommand();
                    cmd.Connection = con;
                    con.Open();
                    cmd.CommandText = "insert into All_Track values('" + infoSchema.TABLE_NAME + "', '" + nTime + "', '0', 0)";
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                    con.Close();
                }

            }

            MessageBox.Show("Task complete!");
        }

        double DirectDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double earthRadius = 3959.00;
            double dLat = ToRadians(lat2 - lat1);
            double dLng = ToRadians(lng2 - lng1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double dist = earthRadius * c;
            double meterConversion = 1609.334;
            return Math.Ceiling(dist * meterConversion);
        }

        double ToRadians(double degrees)
        {
            double radians = degrees * 3.14159265 / 180;
            return radians;
        }

    }
}
