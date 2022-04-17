using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json;
using Triggered.Models;
using Triggered.Services;

namespace Triggered.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExecuteController : ControllerBase
    {
        private ModuleService ModuleService { get; }
        public IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }

        public ExecuteController(ModuleService moduleService, IDbContextFactory<TriggeredDbContext> dbContextFactory)
        {
            ModuleService = moduleService;
            DbContextFactory = dbContextFactory;
        }

        [HttpPost("event")]
        public async Task<IActionResult> ExecuteEvent([FromQuery] string? name, [FromBody] JsonElement? arguments)
        {
            if (name == null)
                return BadRequest("Please supply a valid event name in the query string!");

            try
            {
                await ModuleService.ExecuteModules(name, arguments.HasValue ? arguments.Value.ToString() : string.Empty);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }

            return Ok();
        }

        [HttpPost("testEvent")]
        public async Task<IActionResult> ExecuteTestEvent([FromQuery] int? Id)
        {
            if (Id == null)
                return BadRequest("Please supply a valid event ID in the query string!");

            EventTest? eventTest = (await DbContextFactory.CreateDbContextAsync()).EventTests.FirstOrDefault(eventTest => eventTest.Id == Id);

            if (eventTest == null)
                return NotFound($"Could not find event test with ID {Id}");

            try
            {
                await ModuleService.ExecuteModules(eventTest.Event, eventTest.JsonData ?? string.Empty);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }

            return Ok();
        }

        [HttpPost("module")]
        public async Task<IActionResult> ExecuteModule([FromQuery] int? Id, [FromBody] JsonElement? arguments)
        {
            if (Id == null)
                return BadRequest("Please supply a valid module ID in the query string! (I.e. \"?id=12\")");

            try
            {
                await ModuleService.ExecuteModule((int)Id, arguments.HasValue ? arguments.Value.ToString() : string.Empty);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }

            return Ok();
        }
    }
}
