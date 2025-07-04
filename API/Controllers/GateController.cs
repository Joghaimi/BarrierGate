﻿using API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GateController : ControllerBase
    {
        private readonly AppDbContextAPI _context;
        public GateController(AppDbContextAPI context)
        {
            _context = context;
        }
        [HttpGet("Update")]
        public async Task<IActionResult> update(int Id, DateTime Date, int numberOfOpenCurrectly, int numberOfOpenIllegel,
             int ReachUpperLimitSwitch, int ReachLowerLimitSwitch, int LoopDetector)
        {
            var gateTransaction = new GateTransaction
            {
                Id = Id,
                Date = Date,
                numberOfOpenCurrectly = numberOfOpenCurrectly,
                numberOfOpenIllegel = numberOfOpenIllegel,
                ReachUpperLimitSwitch = ReachUpperLimitSwitch,
                ReachLowerLimitSwitch = ReachLowerLimitSwitch,
                LoopDetector = LoopDetector,
                isSent = false // Assuming this is false by default
            };
            _context.Add(gateTransaction);
            _context.SaveChanges();
            return Ok();
        }
        [HttpGet("UpdateGateActions")]
        public async Task<IActionResult> UpdateGateActions(int Id, DateTime Date, GateSignalStatus GateSignalStatus)
        {
            var GateActions = new GateActions
            {
                Id = Id,
                Date = Date,
                Actions = GateSignalStatus,
                isSent = true // Assuming this is false by default
            };
            _context.Add(GateActions);
            _context.SaveChanges();
            return Ok();
        }
        [HttpDelete("ClearAll")]
        public async Task<IActionResult> ClearAll()
        {
            // Remove all rows from the table
            _context.GateTransactions.RemoveRange(_context.GateTransactions);
            await _context.SaveChangesAsync();
            return Ok("All records deleted successfully.");
        }

        [HttpGet("Open")]
        public async Task<IActionResult> Open(int numberOfIllegelOpenning)
        {
            VariableService.numberOfOpenning += 1;
            VariableService.numberOfIlliegelOpenning += numberOfIllegelOpenning;
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> getOpenningsNumber()
        {
            var today = DateTime.UtcNow.Date;

            // Group by date and sum values
            var dailySums = await _context.GateTransactions
                //.Where(t => t.Date.Date == today)
                .GroupBy(t => t.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalOpenCurrectly = g.Sum(t => t.numberOfOpenCurrectly),
                    TotalOpenIllegel = g.Sum(t => t.numberOfOpenIllegel),
                    TotalUpperLimit = g.Sum(t => t.ReachUpperLimitSwitch),
                    TotalLowerLimit = g.Sum(t => t.ReachLowerLimitSwitch),
                    TotalLoopDetector = g.Sum(t => t.LoopDetector)
                })
                .ToListAsync();

            // Generate HTML
            string html = $@"
    <html>
        <body>
            <h2>Gate Activity Report </h2>
            <table border='1' cellpadding='5' cellspacing='0'>
                <tr>
                    <th>Date</th>
                    <th>Paid Door Opens</th>
                    <th>Unpaid Door Opens</th>
                    <th>Reached Upper Limit</th>
                 
                    <th>Loop Detector Triggers</th>
                </tr>";

            foreach (var row in dailySums)
            {
                html += $@"
                <tr>
                    <td>{row.Date:yyyy-MM-dd}</td>
                    <td>{row.TotalOpenCurrectly}</td>
                    <td>{row.TotalOpenIllegel}</td>
                    <td>{row.TotalUpperLimit}</td>
                    <td>{row.TotalLoopDetector}</td>
                </tr>";
            }

            html += @"
            </table>
        </body>
    </html>";

            return Content(html, "text/html");
        }


        [HttpGet("daily-signal-summary")]
        public async Task<IActionResult> GetDailySignalPivot()
        {
            var data = await _context.GateActions
                .ToListAsync();

            // Group by date
            var groupedByDate = data
                .GroupBy(g => g.Date.Date)
                .OrderBy(g => g.Key)
                .ToList();

            // Get all enum values except Unknown if you want (or keep Unknown)
            var allStatuses = Enum.GetValues(typeof(GateSignalStatus))
                .Cast<GateSignalStatus>()
                .OrderBy(e => (byte)e)
                .ToList();

            // Build HTML header
            string html = @"
    <html>
        <body>
            <h2>Gate Signal Summary by Date</h2>
            <table border='1' cellpadding='5' cellspacing='0'>
                <tr>
                    <th>Date</th>";

            // Add a <th> for each status name
            foreach (var status in allStatuses)
            {
                html += $"<th>{status}</th>";
            }

            html += "</tr>";

            // Add rows per date
            foreach (var dateGroup in groupedByDate)
            {
                html += $"<tr><td>{dateGroup.Key:yyyy-MM-dd}</td>";

                foreach (var status in allStatuses)
                {
                    int count = dateGroup.Count(g => g.Actions == status);
                    html += $"<td>{count}</td>";
                }

                html += "</tr>";
            }

            html += @"
            </table>
        </body>
    </html>";

            return Content(html, "text/html");
        }
    }
}