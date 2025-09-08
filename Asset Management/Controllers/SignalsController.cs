using Asset_Management.DTO;
using Asset_Management.Interfaces;
using Asset_Management.Models;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Asset_Management.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class SignalsController : Controller
    {
        private readonly ISignalsService _service;
        public SignalsController(ISignalsService service)
        {

            _service = service;
        }

        [HttpGet("Asset/{assetId}/AllSignals")]
        public IActionResult GetSignals(int assetId)
        {
            try
            {
                IEnumerable<Signal> signals = _service.GetSignals(assetId);
                return Ok(signals);

            }catch(Exception ex)
            {
                return BadRequest($"Error : {ex.Message}");
            }

        }

        [HttpGet("Asset/{assetId}/Signal/{signalId}")]
        public IActionResult GetSpecificSignal(int assetId, int signalId)
        {
            try
            {
                Signal signal = _service.GetSpecificSignal(assetId, signalId);
                return Ok(signal);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error : {ex.Message}");
            }
        }

        [HttpPost("Asset/{assetId}/AddSignal")]
        [Authorize(Roles ="Admin")]
        public IActionResult AddSignal(int assetId, [FromBody] GlobalSignalDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid Name or Description");
            }
            try
            {
                _service.AddSignal(assetId, request);
                return Ok("Signal added successfully");

            }
            catch(DbUpdateException ex)
            {
                    return BadRequest("Signal already present under the same asset");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }


        }
        [HttpPut("Asset/{assetId}/UpdateSignal/{signalId}")]
        [Authorize(Roles = "Admin")]

        public IActionResult UpdateSignal(int assetId, int signalId ,[FromBody] GlobalSignalDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid Description or Name. Only letters, numbers, and spaces are allowed, max 30 characters.");
            }

            try
            {
                _service.UpdateSignal(assetId, signalId, request);
                return Ok("Signal updated successfully");

            }catch(DbUpdateException ex)
            {
                return BadRequest("Signal with same name already exists");
            }

            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpDelete("Asset/{assetId}/Delete/Signal/{signalId}")]
        [Authorize(Roles = "Admin")]

        public IActionResult DeleteSignal(int signalId, int assetId)
        {
            try
            {
                _service.DeleteSignal(signalId, assetId);
                return Ok("Signal deleted successfully");

            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

    }

    
    
}
