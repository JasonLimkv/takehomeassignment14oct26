using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OneST;

namespace OneST.Web.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Enter a UEN.")]
    public string? Uen { get; set; }

    public bool? IsValidUen { get; private set; }

    public IActionResult OnPost()
    {
        Uen = Uen?.Trim();
        if (Uen?.Length > 10)
            ModelState.AddModelError(nameof(Uen), "Enter no more than 10 characters.");
            
        if (ModelState.IsValid)
            IsValidUen = UenValidator.IsValid(Uen);

        return Page();
    }
}
