using LastBackProje.DataAccesLAyer;
using LastBackProje.Models;
using LastBackProje.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LastBackProje.Areas.Manage.Controllers
{
    [Area("manage")]
    public class BrandController : Controller
    {
        private readonly AppDbContext _context;

        public BrandController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int currentPage=1)
        {
            IQueryable<Brand> queries = _context.Brands
                .Include(b => b.products.Where(p => p.IsDeleted == false))
                .Where(b => b.IsDeleted == false)
                .OrderByDescending(c => c.Id);

            return View(PageNatedList<Brand>.Create(queries,currentPage,5,4));
        }

        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) BadRequest();

            Brand brand=await _context.Brands
                .Include(b=>b.products.Where(p=>p.IsDeleted== false))
                .FirstOrDefaultAsync(b=>b.IsDeleted==false && b.Id==id);

            if(brand == null) return NotFound();

            return View(brand);
        }

        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Brand brand)
        {
            if(!ModelState.IsValid) return View(brand);

            if(await _context.Brands.AnyAsync(b=>b.IsDeleted == false && b.Name.ToLower() == brand.Name.Trim().ToLower()))
            {
                ModelState.AddModelError("Name", $"{brand.Name} already exist");
                return View(brand);
            }

            brand.Name = brand.Name.Trim();

            await _context.Brands.AddAsync(brand);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult>Update(int? id)
        {
            if(id == null) BadRequest();

            Brand brand =await _context.Brands
                .FirstOrDefaultAsync(b=>b.IsDeleted == false && b.Id==id);

            if(brand == null) return NotFound();

            return View(brand);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int? id, Brand brand)
        {
            if(id == null) BadRequest();

            if(id != brand.Id) return BadRequest();

            if (!ModelState.IsValid) return View(brand);

            Brand dbbrand = await _context.Brands
               .FirstOrDefaultAsync(b => b.IsDeleted == false && b.Id == id);

            if(dbbrand == null) return NotFound();

            if (await _context.Brands.AnyAsync(b => b.IsDeleted == false && b.Name.ToLower() == brand.Name.Trim().ToLower() && b.Id != dbbrand.Id))
            {
                ModelState.AddModelError("Name", $"{brand.Name} already exist");
                return View(brand);
            }

            dbbrand.Name=brand.Name.Trim();
            dbbrand.UpdatedBy = "Emrah";
            dbbrand.UpdatedDate= DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) BadRequest();

            Brand brand = await _context.Brands
                .Include(b => b.products.Where(p => p.IsDeleted == false))
                .FirstOrDefaultAsync(b => b.IsDeleted == false && b.Id == id);

            if (brand == null) return NotFound();

            return View(brand);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteBrand(int? id)
        {

            if (id == null) BadRequest();

            Brand brand = await _context.Brands
                .Include(b => b.products.Where(p => p.IsDeleted == false))
                .FirstOrDefaultAsync(b => b.IsDeleted == false && b.Id == id);

            if (brand == null) return NotFound();

            brand.IsDeleted = true;
            brand.DeletedBy = "Emrah";
            brand.DeletedDate= DateTime.Now;

            if(brand.products != null && brand.products.Count() > 0)
            {
                foreach (Product product in brand.products)
                {
                    product.BrandId = null;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }

  
}
