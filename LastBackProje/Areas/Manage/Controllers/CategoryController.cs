using LastBackProje.DataAccesLAyer;
using LastBackProje.Models;
using LastBackProje.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace LastBackProje.Areas.Manage.Controllers
{
    [Area("manage")]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CategoryController(AppDbContext context,IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Index(int currentPage=1)
        {
            IQueryable<Category> queries = _context.Categories
                .Include(c=>c.Products.Where(p=>p.IsDeleted==false))
                .Include(c => c.Children.Where(p => p.IsDeleted == false))
                .Where(c => c.IsDeleted == false && c.IsMain == true);
         

            return View(PageNatedList<Category>.Create(queries,currentPage,5,6));
        }

        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null) return BadRequest();

            Category category=await _context.Categories
                .Include(c => c.Products.Where(p => p.IsDeleted == false))
                .Include(c=>c.Children.Where(ch=>ch.IsDeleted==false))
                .ThenInclude(c => c.Products.Where(p => p.IsDeleted == false))
                .FirstOrDefaultAsync(c=>c.IsDeleted==false && c.Id==id);

            if(category == null) return NotFound();

            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            List<Category>categories=await _context.Categories.Where(c=>c.IsDeleted == false && c.IsMain==true).ToListAsync();

            ViewBag.Categories = categories;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            List<Category> categories = await _context.Categories.Where(c => c.IsDeleted == false && c.IsMain == true).ToListAsync();
            ViewBag.Categories = categories;

            if (!ModelState.IsValid) return View(category);

            if (category.IsMain)
            {
                if (category.File == null)
                {
                    ModelState.AddModelError("File", "File is required");
                    return View(category);
                }

                if(category.File.ContentType != "image/jpeg")
                {
                    ModelState.AddModelError("File", "File type must be .jpg");
                    return View(category);
                }

                if((category.File.Length)/1024 > 30)
                {
                    ModelState.AddModelError("File", "File type must be max 30kb");
                    return View(category);
                }

                //int lastDotIndex=category.File.FileName.LastIndexOf('.');

                //string fileExtenyion = category.File.FileName.Substring(lastDotIndex);

                string fileName = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssfff")+category.File.FileName
                    .Substring(category.File.FileName.LastIndexOf('.'));

                string filePath = Path.Combine(_env.WebRootPath, "assets", "images",fileName);

                //string filePath = _env.WebRootPath + @"\assets\images"+fileName;

                //string filePath = "C:\\Users\\LENOVA\\source\\repos\\LastBackProje\\LastBackProje\\wwwroot\\assets\\images" + fileExtenyion;

                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await category.File.CopyToAsync(fileStream);
                }

                category.Image = fileName;

                category.ParentId = null;


                #region controllerpath
                //try
                //{
                //   await  category.File.CopyToAsync(fileStream);
                //}
                //finally 
                //{ 
                //    fileStream.Dispose();
                //}

                //if(string.IsNullOrWhiteSpace(category.Image))
                //{
                //    ModelState.AddModelError("Image", "Required");

                //    return View(category);
                //}
                #endregion

            }
            else
            {
                if(category.ParentId == null)
                {

                    ModelState.AddModelError("ParentId", "Required");

                    return View(category);
                }

                if(!await _context.Categories.AnyAsync(c=>c.IsDeleted==false && c.IsMain == true && c.Id == category.ParentId))
                {
                    ModelState.AddModelError("ParentId", "Parent is in correct");

                    return View(category);
                }

                category.Image = null;
            }

            if(await _context.Categories.AnyAsync(c=>c.IsDeleted==false && c.Name.ToLower()==category.Name.Trim().ToLower()))
            {
                ModelState.AddModelError("Name", "already exist");

                return View(category);
            }

            category.Name=category.Name.Trim();
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
            

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Update(int? id)
        {
            if(id == null) return BadRequest();

            Category category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted==false);
            
            if(category == null) return NotFound(); 

            ViewBag.Categories = await _context.Categories
                .Where(c=>c.IsDeleted==false && c.IsMain==true)
                .ToListAsync();

            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int? id, Category category)
        {
            

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsDeleted == false && c.IsMain == true)
                .ToListAsync();

            if (id == null) return BadRequest();

            if (id != category.Id) return BadRequest();

            if (!ModelState.IsValid) return View(category);

            Category dbcategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.IsDeleted == false && c.Id == category.Id);

            if (dbcategory == null) return NotFound();

            if (await _context.Categories.AnyAsync(c => c.IsDeleted == false && c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id))
            {
                ModelState.AddModelError("Name", "Already Exists");
                return View(category);
            };

            if (dbcategory.IsMain != category.IsMain)
            {
                ModelState.AddModelError("IsMain", "Dont change");
                return View(category);
            }

            if (dbcategory.IsMain && category.File !=null)
            {

                    if (category.File.ContentType != "image/jpeg")
                    {
                        ModelState.AddModelError("File", "File type must be .jpg");
                        return View(category);
                    }

                    if ((category.File.Length) / 1024 > 30)
                    {
                        ModelState.AddModelError("File", "File type must be max 30kb");
                        return View(category);
                    }


                    string fileName = DateTime.UtcNow.AddHours(4).ToString("yyyyMMddHHmmssfff") + category.File.FileName
                        .Substring(category.File.FileName.LastIndexOf('.'));

                    string filePath = Path.Combine(_env.WebRootPath, "assets", "images", fileName);



                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await category.File.CopyToAsync(fileStream);
                    }

                    dbcategory.Image = fileName;

                    dbcategory.ParentId = null;

            }
            else if(!dbcategory.IsMain ) 
            {
                if (category.ParentId == null)
                {

                    ModelState.AddModelError("ParentId", "Required");

                    return View(category);
                }

                if (!await _context.Categories.AnyAsync(c => c.IsDeleted == false && c.IsMain == true && c.Id == category.ParentId))
                {
                    ModelState.AddModelError("ParentId", "Parent is in correct");

                    return View(category);
                }

                dbcategory.Image = null;
                dbcategory.ParentId = category.ParentId;
            }

            dbcategory.Name = category.Name.Trim();
            dbcategory.UpdatedDate = DateTime.Now;
            dbcategory.UpdatedBy = "Emrah";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


       


        [HttpGet]
        public async Task<IActionResult> DeleteCategory(int? id)
        {
            if (id == null) return BadRequest();

            Category category = await _context.Categories
                .Include(c => c.Products.Where(p => p.IsDeleted == false))
                .Include(c => c.Children.Where(ch => ch.IsDeleted == false))
                .ThenInclude(c => c.Products.Where(p => p.IsDeleted == false))
                .FirstOrDefaultAsync(c => c.IsDeleted == false && c.Id == id);

            if (category == null) return NotFound();

            if(category.Products !=null && category.Products.Count() > 0)
            {
                return BadRequest();
            }

            if(category.Children != null && category.Children.Count() > 0)
            {
                foreach (Category child in category.Children)
                {
                    if(child.Products != null && child.Products.Count() > 0)
                    {
                        return BadRequest();
                    }
                }

                return BadRequest();
            }

            category.IsDeleted = true;
            category.DeletedDate = DateTime.Now;
            category.DeletedBy = "Emrah";
            await _context.SaveChangesAsync();

            if(!string.IsNullOrWhiteSpace(category.Image) )
            {
                string filePath = Path.Combine(_env.WebRootPath, "assets", "images", category.Image);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }


            return RedirectToAction(nameof(Index));
        }
    }
}
