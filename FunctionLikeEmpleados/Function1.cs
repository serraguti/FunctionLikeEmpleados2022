using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace FunctionLikeEmpleados
{
    public static class Function1
    {
        [FunctionName("functionlikeempleados")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("Aplicación para Likes de empleados");

            //VAMOS A RECIBIR EL ID DEL EMPLEADO
            string idempleado = req.Query["idempleado"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            idempleado = idempleado ?? data?.idempleado;

            //COMPROBAMOS SI HEMOS RECIBIDO EL PARAMETRO
            if (idempleado == null)
            {
                //RESPUESTA DE ERROR
                return new BadRequestObjectResult("Necesitamos el número de empleados para el LIKE");
            }
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables().Build();

            string cadenaconexion = config.GetConnectionString("hospitalazure");
            
                //@"Data Source=LOCALHOST;Initial Catalog=HOSPITAL;Persist Security Info=True;User ID=SA;Password=azure";
            SqlConnection cn = new SqlConnection(cadenaconexion);
            string consultaupdate =
                "UPDATE EMP SET SALARIO = SALARIO + 1 WHERE EMP_NO=@EMPNO";
            SqlParameter paramempno = new SqlParameter("@EMPNO", idempleado);
            SqlCommand com = new SqlCommand();
            com.Connection = cn;
            com.CommandType = System.Data.CommandType.Text;
            com.CommandText = consultaupdate;
            com.Parameters.Add(paramempno);
            cn.Open();
            com.ExecuteNonQuery();
            cn.Close();
            com.Parameters.Clear();
            //VAMOS A MOSTRAR LOS DATOS DEL EMPLEADOS MODIFICADO
            string sql = "select * from emp where emp_no=" + idempleado;
            SqlDataAdapter ademp = new SqlDataAdapter(sql, cadenaconexion);
            DataTable tabla = new DataTable();
            ademp.Fill(tabla);
            if (tabla.Rows.Count == 0)
            {
                return new BadRequestObjectResult("El Id de empleado no existe");
            }
            else
            {
                DataRow fila = tabla.Rows[0];
                string mensaje = "El empleado " + fila["APELLIDO"]
                    + " con oficio " + fila["OFICIO"]
                    + " ha incrementado su salario a "
                    + fila["SALARIO"] + ".  Muchas gracias por el Like!!!";
                return new OkObjectResult(mensaje);
            }
        }
    }
}

