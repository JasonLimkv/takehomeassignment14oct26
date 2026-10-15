using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OneST;

namespace OneST.Web.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Enter a UEN.")]
    [StringLength(100, ErrorMessage = "Enter no more than 100 characters.")]
    public string? Uen { get; set; }

    public bool? IsValidUen { get; private set; }

    public IActionResult OnPost()
    {
        if (ModelState.IsValid)
            IsValidUen = UenValidator.IsValid(Uen);

        return Page();
    }
}
