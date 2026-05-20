using System.ComponentModel.DataAnnotations;
using BankTransferMVC.ValidationAttributes;

namespace BankTransferMVC.Models;

/// <summary>
/// Generic "party" sub-VM — used for payer, payee, registrant, holder.
/// Models the Clear Junction <c>individual</c> / <c>corporate</c> wrapper from
/// docs/ClearJunction-Features-And-Fields-Reference.md §2.11.
///
/// The EntityType discriminator drives both client-side visibility (via data-cj-when) and
/// server-side validation (via [CjRequiredIf]).
/// </summary>
public class CjPartyViewModel
{
    [Required, Display(Name = "Entity type")]
    public string EntityType { get; set; } = "individual";

    // ----- individual fields -----
    [CjRequiredIf("EntityType", "individual"), Display(Name = "First name")]
    public string? FirstName { get; set; }

    [CjRequiredIf("EntityType", "individual"), Display(Name = "Last name")]
    public string? LastName { get; set; }

    [CjRequiredIf("EntityType", "individual"), DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateTime? BirthDate { get; set; } = new DateTime(1990, 1, 1);

    /// <summary>Mandatory on SWIFT individual entities — enforced by parent IValidatableObject.</summary>
    [Display(Name = "Place of birth")]
    public string? BirthPlace { get; set; }

    [Display(Name = "Country of birth")]
    public string? BirthCountry { get; set; }

    [Display(Name = "Tax number")]
    public string? TaxNumber { get; set; }

    [Display(Name = "Tax country")]
    public string? TaxCountry { get; set; }

    // ----- corporate fields -----
    [CjRequiredIf("EntityType", "corporate"), Display(Name = "Legal name")]
    public string? CorporateName { get; set; }

    [CjRequiredIf("EntityType", "corporate"), Display(Name = "Registration number")]
    public string? RegistrationNumber { get; set; }

    [CjRequiredIf("EntityType", "corporate"), Display(Name = "Incorporation country")]
    public string? IncorporationCountry { get; set; }

    [DataType(DataType.Date), Display(Name = "Incorporation date")]
    public DateTime? IncorporationDate { get; set; } = new DateTime(2010, 1, 1);

    /// <summary>Legal Entity Identifier (ISO 17442) — required on corporate FPS/CHAPS/SWIFT.
    /// Enforced by parent IValidatableObject (depends on Rail outside this sub-VM).</summary>
    [Display(Name = "LEI (ISO 17442)")]
    public string? Lei { get; set; }

    // ----- shared -----
    [EmailAddress, Display(Name = "Contact email")]
    public string? Email { get; set; }

    [Phone, Display(Name = "Phone (E.164)")]
    public string? Phone { get; set; }

    // address
    [Display(Name = "Country")] public string? Country { get; set; }
    [Display(Name = "Street")] public string? Street { get; set; }
    [Display(Name = "City")] public string? City { get; set; }
    [Display(Name = "ZIP / Postal code")] public string? Zip { get; set; }

    public string DisplayName() =>
        string.Equals(EntityType, "corporate", StringComparison.OrdinalIgnoreCase)
            ? CorporateName ?? ""
            : string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>
    /// Address <see cref="Country"/> is separate from IBAN country / incorporation country.
    /// When the address country lookup was not posted (e.g. Tom Select) copy from the best available source.
    /// </summary>
    public void FillAddressCountryIfEmpty(string? ibanOrPrimary, string? entitySpecific = null)
    {
        if (!string.IsNullOrWhiteSpace(Country)) return;
        if (!string.IsNullOrWhiteSpace(ibanOrPrimary))
            Country = ibanOrPrimary.Trim();
        else if (!string.IsNullOrWhiteSpace(entitySpecific))
            Country = entitySpecific.Trim();
    }
}

/// <summary>SWIFT institution block (POST .../bankTransfer/swift). All fields required on SWIFT rail.</summary>
public class CjInstitutionViewModel
{
    [Display(Name = "BIC / SWIFT code")]
    public string? BankSwiftCode { get; set; }

    [Display(Name = "Bank legal name")]
    public string? BankName { get; set; }

    [Display(Name = "Bank country")]
    public string? Country { get; set; }

    [Display(Name = "Bank city")] public string? City { get; set; }
    [Display(Name = "Bank street")] public string? Street { get; set; }
    [Display(Name = "Bank ZIP")] public string? Zip { get; set; }

    [Display(Name = "Clearing system code")]
    public string? ClearingSystemIdCode { get; set; }

    [Display(Name = "Clearing member id")]
    public string? MemberId { get; set; }
}
