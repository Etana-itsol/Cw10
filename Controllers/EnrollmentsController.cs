using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cw5.DTOs.Requests;
using Cw5.DTOs.Responses;
using Cw5.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cw5.Controllers
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private IStudentDbServices _service;

        public EnrollmentsController(IStudentDbServices service)
        {
            _service = service;
        }
        

        [HttpPost]
        public IActionResult EnrollStudent(EnrollStudentRequest request)
        {
            var response = _service.EnrollStudent(request);

            return Ok(response);
        }

     
        [HttpPost("promotions")]
        public IActionResult PromoteStudents(PromoteStudentRequest request)
        {
            PromoteStudentRequest res = _service.PromoteStudents(request);
            res.Semester = res.Semester + 1;
            return Ok(res);
        }
        
    }
}