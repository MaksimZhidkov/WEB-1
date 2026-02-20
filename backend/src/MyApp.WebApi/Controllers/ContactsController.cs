using Microsoft.AspNetCore.Mvc;

namespace MyApp.WebApi.Controllers;

[ApiController]
[Route("api/contacts")]
public sealed class ContactsController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Company = "ООО «Пример»",
            Address = "Нижний Новгород, ул. Примерная, 1",
            Phone = "+7 (999) 000-00-00",
            Email = "info@example.com"
        });
    }
}
