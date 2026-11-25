using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public StudentsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private SqlConnection CreateConnection() =>
        new(_configuration.GetConnectionString("DefaultConnection"));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetAll()
    {
        var students = new List<Student>();
        await using var con = CreateConnection();
        const string sql = "SELECT StudentId, StudentName, Department, Email, CGPA FROM Students";
        await using var cmd = new SqlCommand(sql, con);
        await con.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            students.Add(new Student
            {
                StudentId = reader.GetInt32(0),
                StudentName = reader.GetString(1),
                Department = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                CGPA = reader.GetDecimal(4)
            });
        }
        return Ok(students);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Student>> GetById(int id)
    {
        await using var con = CreateConnection();
        const string sql = "SELECT StudentId, StudentName, Department, Email, CGPA FROM Students WHERE StudentId=@Id";
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@Id", id);
        await con.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return NotFound(new { message = "Student not found." });

        return Ok(new Student
        {
            StudentId = reader.GetInt32(0),
            StudentName = reader.GetString(1),
            Department = reader.IsDBNull(2) ? "" : reader.GetString(2),
            Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
            CGPA = reader.GetDecimal(4)
        });
    }

    [HttpPost]
    public async Task<ActionResult> Create(Student student)
    {
        if (string.IsNullOrWhiteSpace(student.StudentName))
            return BadRequest(new { message = "Student name is required." });

        await using var con = CreateConnection();
        const string sql = @"INSERT INTO Students(StudentName, Department, Email, CGPA)
                             VALUES(@Name, @Department, @Email, @CGPA);
                             SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@Name", student.StudentName);
        cmd.Parameters.AddWithValue("@Department", (object?)student.Department ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Email", (object?)student.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CGPA", student.CGPA);
        await con.OpenAsync();
        var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        student.StudentId = newId;
        return CreatedAtAction(nameof(GetById), new { id = newId }, student);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Student student)
    {
        if (id != student.StudentId) return BadRequest(new { message = "Route ID and StudentId must match." });

        await using var con = CreateConnection();
        const string sql = @"UPDATE Students SET StudentName=@Name, Department=@Department,
                             Email=@Email, CGPA=@CGPA WHERE StudentId=@Id";
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Name", student.StudentName);
        cmd.Parameters.AddWithValue("@Department", (object?)student.Department ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Email", (object?)student.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CGPA", student.CGPA);
        await con.OpenAsync();
        if (await cmd.ExecuteNonQueryAsync() == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var con = CreateConnection();
        await using var cmd = new SqlCommand("DELETE FROM Students WHERE StudentId=@Id", con);
        cmd.Parameters.AddWithValue("@Id", id);
        await con.OpenAsync();
        if (await cmd.ExecuteNonQueryAsync() == 0) return NotFound();
        return NoContent();
    }
}
