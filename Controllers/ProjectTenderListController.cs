using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KLENZ.Data;
using KLENZ.Models;

namespace KLENZ.Controllers
{
    public class ProjectTenderListController : Controller
    {
        private readonly KLENZDbContext _context;

        public ProjectTenderListController(KLENZDbContext context)
        {
            _context = context;
        }

        // GET: ProjectTenderList
        public async Task<IActionResult> Index()
        {
            var list = await _context.Project_TenderList
                .Include(p => p.Company)
                .Include(p => p.FinancialYear)
                .Include(p => p.GSTType)
                .OrderByDescending(p => p.CreatedDateTime)
                .Select(p => new Project_TenderList
                {
                    Id = p.Id,
                    FyYear = p.FyYear,
                    WorkOrderDate = p.WorkOrderDate,
                    CompanyNameId = p.CompanyNameId,
                    CustomerDetails = p.CustomerDetails,
                    WorkDetails = p.WorkDetails,
                    ProjectCost = p.ProjectCost,
                    GSTTypeId = p.GSTTypeId,
                    Total = p.Total,
                    WorkDuration = p.WorkDuration,
                    Remarks = p.Remarks,
                    CreatedDateTime = p.CreatedDateTime,
                    CreatedUserId = p.CreatedUserId,
                    CreatedUserName = _context.Users
                        .Where(u => u.Id == p.CreatedUserId)
                        .Select(u => u.UserName ?? u.Email)
                        .FirstOrDefault() ?? "System",
                    FinancialYear = p.FinancialYear,
                    Company = p.Company,
                    GSTType = p.GSTType
                })
                .ToListAsync();

            return View(list);
        }

        // GET: ProjectTenderList/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project_TenderList = await _context.Project_TenderList
                .Include(p => p.Company)
                .Include(p => p.FinancialYear)
                .Include(p => p.GSTType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project_TenderList == null)
            {
                return NotFound();
            }

            return View(project_TenderList);
        }

        // GET: ProjectTenderList/Create
        public IActionResult Create()
        {
            ViewData["CompanyNameId"] = new SelectList(_context.CompanyName, "Id", "FullName");
            ViewData["FyYear"] = new SelectList(_context.FinancialYear, "Id", "FyYear");
            ViewData["GSTTypeId"] = new SelectList(_context.GSTTypes, "Id", "GSTType");
            return View();
        }

        // POST: ProjectTenderList/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FyYear,WorkOrderDate,CompanyNameId,CustomerDetails,WorkDetails,ProjectCost,GSTTypeId,Total,WorkDuration,Remarks,CreatedDateTime,CreatedUserId")] Project_TenderList project_TenderList)
        {
            // Set on the server so nobody has to type who created the record or when
            project_TenderList.CreatedDateTime = DateTime.Now;
            project_TenderList.CreatedUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            ModelState.Remove(nameof(project_TenderList.CreatedDateTime));
            ModelState.Remove(nameof(project_TenderList.CreatedUserId));

            if (ModelState.IsValid)
            {
                _context.Add(project_TenderList);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CompanyNameId"] = new SelectList(_context.CompanyName, "Id", "FullName", project_TenderList.CompanyNameId);
            ViewData["FyYear"] = new SelectList(_context.FinancialYear, "Id", "FyYear", project_TenderList.FyYear);
            ViewData["GSTTypeId"] = new SelectList(_context.GSTTypes, "Id", "GSTType", project_TenderList.GSTTypeId);
            return View(project_TenderList);
        }

        // GET: ProjectTenderList/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project_TenderList = await _context.Project_TenderList.FindAsync(id);
            if (project_TenderList == null)
            {
                return NotFound();
            }
            ViewData["CompanyNameId"] = new SelectList(_context.CompanyName, "Id", "FullName", project_TenderList.CompanyNameId);
            ViewData["FyYear"] = new SelectList(_context.FinancialYear, "Id", "FyYear", project_TenderList.FyYear);
            ViewData["GSTTypeId"] = new SelectList(_context.GSTTypes, "Id", "GSTType", project_TenderList.GSTTypeId);
            return View(project_TenderList);
        }

        // POST: ProjectTenderList/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FyYear,WorkOrderDate,CompanyNameId,CustomerDetails,WorkDetails,ProjectCost,GSTTypeId,Total,WorkDuration,Remarks,CreatedDateTime,CreatedUserId")] Project_TenderList project_TenderList)
        {
            if (id != project_TenderList.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project_TenderList);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Project_TenderListExists(project_TenderList.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CompanyNameId"] = new SelectList(_context.CompanyName, "Id", "FullName", project_TenderList.CompanyNameId);
            ViewData["FyYear"] = new SelectList(_context.FinancialYear, "Id", "FyYear", project_TenderList.FyYear);
            ViewData["GSTTypeId"] = new SelectList(_context.GSTTypes, "Id", "GSTType", project_TenderList.GSTTypeId);
            return View(project_TenderList);
        }

        // GET: ProjectTenderList/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project_TenderList = await _context.Project_TenderList
                .Include(p => p.Company)
                .Include(p => p.FinancialYear)
                .Include(p => p.GSTType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (project_TenderList == null)
            {
                return NotFound();
            }

            return View(project_TenderList);
        }

        // POST: ProjectTenderList/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project_TenderList = await _context.Project_TenderList.FindAsync(id);
            if (project_TenderList != null)
            {
                _context.Project_TenderList.Remove(project_TenderList);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Project_TenderListExists(int id)
        {
            return _context.Project_TenderList.Any(e => e.Id == id);
        }
    }
}
