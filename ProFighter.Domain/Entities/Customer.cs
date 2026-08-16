using ProFighter.Domain.Common;
using ProFighter.Domain.Enums;

namespace ProFighter.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; private set; }
    public string MobileNumber { get; private set; }
    public string? Email { get; private set; }
    public Guid? RekazCustomerId { get; private set; }
    public CustomerSource Source { get; private set; }
    public int LoyaltyPointsBalance { get; private set; }
    public bool IsFirstLogin { get; private set; } = true;



	private Customer() : base()
    {
        Name = null!;
        MobileNumber = null!;
    }
	public void MarkPasswordAsChanged()
	{
	
		MarkAsUpdated();

	}

	public Customer(Guid id, string name, string mobileNumber, CustomerSource source, string? email = null, Guid? rekazCustomerId = null, bool isFirstLogin = true) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(mobileNumber))
            throw new ArgumentException("Mobile number cannot be empty.", nameof(mobileNumber));


		// Shared primary key pattern: explicitly override the auto-generated Id with the provided Identity User Id
		Id = id;
        Name = name;
        MobileNumber = mobileNumber;
        Source = source;
        Email = email;
        RekazCustomerId = rekazCustomerId;
        LoyaltyPointsBalance = 0;
        IsFirstLogin = isFirstLogin;
        CreatedAt = DateTime.UtcNow;
    }

    // Constructor for self-registration (email) - never requires first login
    public Customer(Guid id, string name, string mobileNumber, string? email, Guid? rekazCustomerId) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(mobileNumber))
            throw new ArgumentException("Mobile number cannot be empty.", nameof(mobileNumber));

        Id = id;
        Name = name;
        MobileNumber = mobileNumber;
        Source = CustomerSource.EmailRegistration;
        Email = email;
        RekazCustomerId = rekazCustomerId;
        LoyaltyPointsBalance = 0;
        IsFirstLogin = false; // Self-registered users set their own password
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string mobileNumber, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(mobileNumber))
            throw new ArgumentException("Mobile number cannot be empty.", nameof(mobileNumber));

        Name = name;
        MobileNumber = mobileNumber;
        Email = email;
        MarkAsUpdated();
    }

    public void SyncRekazId(Guid rekazCustomerId)
    {
        RekazCustomerId = rekazCustomerId;
        MarkAsUpdated();
    }

    public void AdjustLoyaltyPoints(int points)
    {
        LoyaltyPointsBalance += points;
        if (LoyaltyPointsBalance < 0)
        {
            LoyaltyPointsBalance = 0;
        }
        MarkAsUpdated();
    }

    public void MarkFirstLoginCompleted()
    {
        IsFirstLogin = false;
        MarkAsUpdated();
    }
}
