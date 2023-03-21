using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KanbanAPI.Models;
using KanbanAPI.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace KanbanAPI.Controllers
{
    //[Authorize] // Nur autorisierte Benutzer können auf die Karten zugreifen
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CardController : ControllerBase
    {
        private readonly KanbanDbContext _context;

        public CardController(KanbanDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult CreateEditCard([FromBody] Ticket ticket)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ticket.Id == 0)
            {
                _context.Tickets.Add(ticket);
            }
            else
            {
                var ticketInDb = _context.Tickets.Find(ticket.Id);

                if (ticketInDb == null)
                    return NotFound();

                _context.Entry(ticketInDb).CurrentValues.SetValues(ticket);
            }

            _context.SaveChanges();

            return Ok(ticket);
        }

        [HttpGet]
        public IActionResult GetCard(int cardId)
        {
            var result = _context.Tickets.Find(cardId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete]
        public IActionResult DeleteCard(int cardId)
        {
            var result = _context.Tickets.Find(cardId);

            if (result == null)
                return NotFound();

            _context.Tickets.Remove(result);
            _context.SaveChanges();

            return Ok($"Card with Id: {cardId} was successfully removed");
        }

        [HttpGet]
        public IActionResult GetAllCards()
        {
            var result = _context.Tickets.ToList();

            return Ok(result);
        }
    }
}
