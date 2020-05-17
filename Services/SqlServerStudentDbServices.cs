using Cw5.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Cw5.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Cw5.DTOs.Responses;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
//using Microsoft.Data.SqlClient;//?

namespace Cw5.Services
{
    public class SqlServerStudentDbServices : IStudentDbServices
    {
        private readonly s18725Context _dbContext;

        public SqlServerStudentDbServices(s18725Context context)
        {
            this._dbContext = context;
        }

        public Enrollment EnrollStudent(EnrollStudentRequest request)
        {
            /*
            using var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True");
            con.Open();
            using var transaction = con.BeginTransaction();

            //check if studies exists
            if (!CheckStudies(request.Studies, con, transaction))
            {
                transaction.Rollback();
                throw new Exception ( "Studies does not exist.");
            }

            //get (or create and get) the enrollment
            var enrollment = NewEnrollment(request.Studies, 1, con, transaction);
            if (enrollment == null)
            {
                CreateEnrollment(request.Studies, 1, DateTime.Now, con, transaction);
                enrollment = NewEnrollment(request.Studies, 1, con, transaction);
            }

            //check if provided index number is unique
            if (GetStudent(request.IndexNumber) != null)
            {
                transaction.Rollback();
                throw new Exception( $"Index number ({request.IndexNumber}) is not unique.");
            }

            //create a student and commit the transaction
            CreateStudent(request.IndexNumber, request.FirstName, request.LastName, request.BirthDate, enrollment.IdEnrollment, con, transaction);
            transaction.Commit();

            //return Enrollment object
            */
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.IndexNumber) ||
                string.IsNullOrWhiteSpace(request.Studies) )
            {
                throw new Exception("Niepodano wszystkich informacji");
               
            }
            var studies = _dbContext.Studies.FirstOrDefault(e => e.Name == request.Studies);

            if (studies == null)
            {
                throw new Exception("Studia nie istnieją");
            }

            var enrollment = _dbContext.Enrollment
                                                  .Where(e => e.IdStudy == studies.IdStudy && e.Semester == 1)
                                                  .OrderByDescending(e => e.StartDate)
                                                  .FirstOrDefault();

            var checkst = _dbContext.Student.FirstOrDefault(e => e.IndexNumber == request.IndexNumber);
            if (checkst != null)
                throw new Exception("Taki student juz istnieje!");

            if (enrollment == null)
            {
                enrollment = new Enrollment()
                {
                    IdEnrollment = _dbContext.Enrollment.Max(e => e.IdEnrollment) + 1,
                    Semester = 1,
                    IdStudy = studies.IdStudy,
                    StartDate = DateTime.Now
                };
                _dbContext.Enrollment.Add(enrollment);
            }


            var student = new Student()
            {
                IndexNumber = request.IndexNumber,
                BirthDate = Convert.ToDateTime(request.BirthDate),
                FirstName = request.FirstName,
                LastName = request.LastName,
                IdEnrollment = enrollment.IdEnrollment
            };

            _dbContext.Student.Add(student);
            _dbContext.SaveChanges();
            return enrollment;
        }
    
        public PromoteStudentRequest PromoteStudents(PromoteStudentRequest request)
        {

            var semestr = new SqlParameter("@Semester",request.Semester);
            var name = new SqlParameter("@Name",request.Studies);
           // semestr.Value = request.Semester;
           // name.Value = request.Studies;
            _dbContext.Database.ExecuteSqlRaw("EXEC PromoteStudents @Name, @Semester",name,semestr);
            //_dbContext.Database.ExecuteSqlRaw($"Execute PromoteStudents @Name={name}, @Semester={semestr}");

            return request;
        }

        public Student GetStudent(string id)
        {

            using (SqlConnection con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True"))
            using (SqlCommand com = new SqlCommand())
            {
                com.Connection = con;
                com.CommandText = "SELECT IndexNumber,FirstName,LastName,BirthDate,Name,Semester FROM Student S JOIN Enrollment E on S.IdEnrollment = E.IdEnrollment JOIN Studies St on E.IdStudy = St.IdStudy WHERE IndexNumber = @index";
                com.Parameters.AddWithValue("index", id);

                con.Open();
                var dr = com.ExecuteReader();
                if (dr.Read())
                {
                    Student st = new Student
                    {
                        IndexNumber = dr["IndexNumber"].ToString(),
                        FirstName = dr["FirstName"].ToString(),
                        LastName = dr["LastName"].ToString(),
                        BirthDate = DateTime.Parse(dr["BirthDate"].ToString()),
                        //Studies = dr["Name"].ToString(),
                        //Semester = int.Parse(dr["Semester"].ToString()),
                    };
                    return st;
                }
                return null;
            }
        }


        public IEnumerable<Student> GetStudents() 
        {
            return _dbContext.Student.ToList();
        }

        public Student UpdateStudent(Student student)
        {
            try
            {
                var st = _dbContext.Student.FirstOrDefault(student => student.IndexNumber.Equals(student.IndexNumber));
                if (st == null)
                    return null;


                _dbContext.Attach(student);
                _dbContext.Entry(student).Property("LastName").IsModified = true;
                _dbContext.Entry(student).Property("FirstName").IsModified = true;

                //_dbContext.Entry(student).State = EntityState.Modified;
                //_dbContext.Update(student);
                _dbContext.SaveChanges();
            }

            catch (Exception)
            {

                throw;
            }

            return student;
        }

        public Student DeleteStudent(Student student)
        {
            if (student.IndexNumber == null)
            {
                return null;
            }

            var st = _dbContext.Student.FirstOrDefault(student => student.IndexNumber.Equals(student.IndexNumber));
            if (st == null)
                return null;

            _dbContext.Remove(student);
            _dbContext.SaveChanges();
            return student;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        private bool CheckStudies(string name, SqlConnection con, SqlTransaction transaction) 
        {
            using var cmd = new SqlCommand
            {
                Connection = con,
                Transaction = transaction,
                CommandText = @"SELECT 1 from Studies s WHERE s.Name = @name;"
            };
            cmd.Parameters.AddWithValue("name", name);
            using var dr = cmd.ExecuteReader();
            return dr.Read();
        }
        private Enrollment NewEnrollment(string studiesName, int semester, SqlConnection con, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand
            {
                Connection = con,
                Transaction = transaction,
                CommandText = @"SELECT TOP 1 e.IdEnrollment, e.IdStudy, e.StartDate
                                FROM Enrollment e JOIN Studies s ON e.IdStudy=s.IdStudy
                                WHERE e.Semester = @Semester AND s.Name = @Name
                                ORDER BY IdEnrollment DESC;"
            };

            cmd.Parameters.AddWithValue("Name", studiesName);
            cmd.Parameters.AddWithValue("Semester", semester);

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new Enrollment
                {
                    IdEnrollment = int.Parse(dr["IdEnrollment"].ToString()),
                    Semester = semester,
                    IdStudy = int.Parse(dr["IdStudy"].ToString()),
                    StartDate = DateTime.Parse(dr["StartDate"].ToString()),
                };
            }
            return null;
        }
        private void CreateEnrollment(string studiesName, int semester, DateTime startDate, SqlConnection con, SqlTransaction transaction)
        {
            using var cmd = new SqlCommand
            {
                Connection = con,
                Transaction = transaction,
                CommandText = @"INSERT INTO Enrollment(IdEnrollment, IdStudy, StartDate, Semester)
                                VALUES ((SELECT ISNULL(MAX(e.IdEnrollment)+1,1) FROM Enrollment e), 
		                                (SELECT s.IdStudy FROM Studies s WHERE s.Name = @Name), 
		                                @StartDate,
		                                @Semester);"
            };

            cmd.Parameters.AddWithValue("Name", studiesName);
            cmd.Parameters.AddWithValue("Semester", semester);
            cmd.Parameters.AddWithValue("StartDate", startDate);
            cmd.ExecuteNonQuery();
        }
        private void CreateStudent(string indexNumber, string firstName, string lastName, DateTime BirthDate, int idEnrollment, SqlConnection sqlConnection = null, SqlTransaction transaction = null)
        {
            using var cmd = new SqlCommand
            {
                CommandText = @"INSERT INTO Student(IndexNumber, FirstName, LastName, BirthDate, IdEnrollment)
                                VALUES (@IndexNumber, @FirstName, @LastName, @BirthDate, @IdEnrollment);"
            };
            cmd.Parameters.AddWithValue("IndexNumber", indexNumber);
            cmd.Parameters.AddWithValue("FirstName", firstName);
            cmd.Parameters.AddWithValue("LastName", lastName);
            cmd.Parameters.AddWithValue("BirthDate", BirthDate);
            cmd.Parameters.AddWithValue("IdEnrollment", idEnrollment);

            if (sqlConnection == null)
            {
                using var con = new SqlConnection("Data Source=db-mssql;Initial Catalog=s18725;Integrated Security=True");
                con.Open();
                cmd.Connection = con;
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.Connection = sqlConnection;
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }
        }

     
    }
    
}
