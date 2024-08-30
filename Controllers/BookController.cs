using BookMgr.Data;
using BookMgr.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.Net;
using System.Security.Policy;

namespace BookMgr.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                      .Include(b => b.Publisher)
                      .Include(b => b.BookAuthors)
                      .ThenInclude(ba => ba.Author)
                      .ToListAsync();
            var bookViewModels = books.Select(book => new BookViewModel
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                PublicationDate = book.PublicationDate,
                PublisherName = book.Publisher.Name,
                AuthorIds = book.BookAuthors.Select(ba => ba.Author.Id).ToArray(),
                Authors = book.BookAuthors.Select(ba => new SelectListItem
                {
                    Text = ba.Author.Name,
                    Value = ba.Author.Id.ToString()
                }).ToList()

            }).ToList();

            return View(bookViewModels);
        }

       // Details : View details of a specific book
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = await _context.Books
                        .Include(b => b.Publisher)
                         .Include(b => b.BookAuthors)
                          .ThenInclude(ba => ba.Author)
                          .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }
            var viewModel = new BookViewModel
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                PublicationDate = book.PublicationDate,
                PublisherName = book.Publisher.Name,
                AuthorIds = book.BookAuthors.Select(ba => ba.Author.Id).ToArray(),
                Authors = book.BookAuthors.Select(ba => new SelectListItem
                {
                    Text = ba.Author.Name,
                    Value = ba.Author.Id.ToString()
                }).ToList()
            };
            return View(viewModel);
        }
        //Show the form to create a new book
        public IActionResult Create()
        {
            var viewModel = new BookViewModel
            {
                Publishers = _context.Publishers
                      .Select(p => new SelectListItem
                      {
                          Value = p.Id.ToString(),
                          Text = p.Name
                      }).ToList(),
                Authors = _context.Authors
                   .Select(a => new SelectListItem
                   {
                       Value = a.Id.ToString(),
                       Text = a.Name
                   }).ToList()
            };
            return View(viewModel);
        }
        //Save the new book to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"Key: {key}, Error{error.ErrorMessage}");
                    }
                }
                //Map the viewModel to the book 
                var book = new Book
                {
                    Title = viewModel.Title,
                    ISBN = viewModel.ISBN,
                    PublicationDate = viewModel.PublicationDate,
                    PublisherId = viewModel.PublisherId
                };

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                foreach (var authorId in viewModel.AuthorIds)
                {
                    var bookAuthor = new BookAuthor
                    {
                        BookId = book.Id,
                        AuthorId = authorId
                    };
                    _context.BookAuthors.Add(bookAuthor);
                }


                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            viewModel.Publishers = _context.Publishers
                       .Select(p => new SelectListItem
                       {
                           Value = p.Id.ToString(),
                           Text = p.Name
                       }).ToList();
            viewModel.Authors = _context.Authors
                   .Select(a => new SelectListItem
                   {
                       Value = a.Id.ToString(),
                       Text = a.Name
                   }).ToList();
            return View(viewModel);

        }

        //Edit the book (get )
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var book = await _context.Books
                    .Include(b => b.Publisher)
                    .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                    .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }
            var viewModel = new BookViewModel
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                PublicationDate = book.PublicationDate,
                PublisherId = book.PublisherId,
                PublisherName = book.Publisher.Name,
                AuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToArray(),
                Publishers = _context.Publishers.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }).ToList(),
                Authors = _context.Authors.Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Name
                }).ToList()
            };
            return View(viewModel);
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                try
                {
                    var book = await _context.Books
                      .Include(b => b.BookAuthors)
                      .FirstOrDefaultAsync(b => b.Id == id);
                    if (book == null) { return NotFound(); }
                    book.Title = viewModel.Title;
                    book.ISBN = viewModel.ISBN;
                    book.PublicationDate = viewModel.PublicationDate;
                    book.PublisherId = viewModel.PublisherId;

                    _context.BookAuthors.RemoveRange(book.BookAuthors);
                    foreach (var authorId in viewModel.AuthorIds)
                    {
                        book.BookAuthors.Add(new BookAuthor
                        {
                            BookId = book.Id,
                            AuthorId = authorId
                        });

                    }
                    _context.Update(book);
                    await _context.SaveChangesAsync();

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(viewModel.Id))
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
            viewModel.Publishers = _context.Publishers.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            }).ToList();
            viewModel.Authors = _context.Authors.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Name
            }).ToList();
            return View(viewModel);

        }
    }
}
