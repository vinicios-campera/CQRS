using Application.Commands.Order;
using Application.Queries.Order;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(IMediator mediator) : ControllerBase
    {
        /// <summary>
        /// Cria um novo pedido
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
        {
            if (command == null)
                return BadRequest("Pedido inválido.");

            var orderId = await mediator.Send(command);
            return CreatedAtAction(nameof(GetOrderById), new { id = orderId }, new { orderId });
        }

        /// <summary>
        /// Obtém um pedido pelo ID (usando a base de leitura no MongoDB)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var query = new GetOrderByIdQuery(id);
            var order = await mediator.Send(query);

            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }
}