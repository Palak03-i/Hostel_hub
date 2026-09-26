using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hostel_hub.Services;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Students")]
    public class StudentsController : Controller
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search, int? hostelId)
        {
            var students = await _studentService.GetAllStudentsAsync(search, hostelId);
            return View(students);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }
    }
}