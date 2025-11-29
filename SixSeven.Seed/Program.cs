using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SixSeven.Data;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
    })
    .Build();

using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

Console.WriteLine("Applying migrations...");
await db.Database.MigrateAsync();

Console.WriteLine("Resetting data...");
await ResetDataAsync(db);

Console.WriteLine("Seeding data...");
await SeedAsync(db);

Console.WriteLine("Done.");

static async Task ResetDataAsync(AppDbContext db)
{
    // Delete children first to satisfy FK constraints
    await db.VolunteerHistories.ExecuteDeleteAsync();
    await db.Notifications.ExecuteDeleteAsync();
    await db.Events.ExecuteDeleteAsync();
    await db.Set<UserProfile>().ExecuteDeleteAsync();
    await db.Users.ExecuteDeleteAsync();

    await db.SaveChangesAsync();
    Console.WriteLine("All existing seed data deleted.");
}

static async Task SeedAsync(AppDbContext db)
{
    // users + profiles
    var users = await SeedUsersAsync(db);

    // events
    var events = await SeedEventsAsync(db);

    // notifications
    await SeedNotificationsAsync(db, users);

    // volunteer history
    await SeedVolunteerHistoryAsync(db, users, events);
}

static async Task<List<UserCredentials>> SeedUsersAsync(AppDbContext db)
{
    // 2 admins, 6 volunteers
    var users = new List<UserCredentials>();

    // NOTE: PasswordHash is dev-only placeholder
    var admin1 = new UserCredentials(
        email: "admin1@example.com",
        passwordHash: "DEV_ONLY_HASH",
        role: UserRole.Admin);

    admin1.AttachProfile(new UserProfile(
        userCredentialsId: admin1.Id,
        firstName: "Alex",
        lastName: "Admin",
        addressOne: "100 Admin Plaza",
        city: "Houston",
        state: "TX",
        zipCode: "77001",
        preferences: "Prefers dashboards and reporting",
        skills: new[] { VolunteerSkill.ITSupport, VolunteerSkill.EventPlanning },
        availability: new[] { "2025-12-01T09:00:00Z", "2025-12-03T14:00:00Z" }
    ));
    users.Add(admin1);

    var admin2 = new UserCredentials(
        email: "admin2@example.com",
        passwordHash: "DEV_ONLY_HASH",
        role: UserRole.Admin);

    admin2.AttachProfile(new UserProfile(
        userCredentialsId: admin2.Id,
        firstName: "Priya",
        lastName: "Coordinator",
        addressOne: "200 Admin Way",
        city: "Houston",
        state: "TX",
        zipCode: "77002",
        preferences: "Loves scheduling and logistics",
        skills: new[] { VolunteerSkill.Construction, VolunteerSkill.EventPlanning },
        availability: new[] { "2025-12-02T10:00:00Z", "2025-12-05T17:00:00Z" }
    ));
    users.Add(admin2);

    // Volunteers
    var v1 = new UserCredentials(
        email: "alex.johnson@example.com",
        passwordHash: "DEV_ONLY_HASH",
        role: UserRole.Volunteer);
    v1.AttachProfile(new UserProfile(
        userCredentialsId: v1.Id,
        firstName: "Alex",
        lastName: "Johnson",
        addressOne: "101 Maple St",
        city: "Houston",
        state: "TX",
        zipCode: "77003",
        preferences: "Enjoys outdoor cleanups",
        skills: new[] { VolunteerSkill.Gardening, VolunteerSkill.Translation },
        availability: new[] { "2025-12-02T09:00:00Z", "2025-12-06T08:00:00Z" }
    ));
    users.Add(v1);

    var v2 = new UserCredentials(
        email: "priya.shah@example.com",
        passwordHash: "DEV_ONLY_HASH",
        role: UserRole.Volunteer);
    v2.AttachProfile(new UserProfile(
        userCredentialsId: v2.Id,
        firstName: "Priya",
        lastName: "Shah",
        addressOne: "202 Cedar Ln",
        city: "Houston",
        state: "TX",
        zipCode: "77004",
        preferences: "Enjoys working with children",
        skills: new[] { VolunteerSkill.ChildCare, VolunteerSkill.ElderlyCare },
        availability: new[] { "2025-12-03T13:00:00Z", "2025-12-07T15:00:00Z" }
    ));
    users.Add(v2);

    var v3 = new UserCredentials(
        email: "marco.lee@example.com",
        passwordHash: "DEV_ONLY_HASH",
        role: UserRole.Volunteer);
    v3.AttachProfile(new UserProfile(
        userCredentialsId: v3.Id,
        firstName: "Marco",
        lastName: "Lee",
        addressOne: "303 Oak Dr",
        city: "Houston",
        state: "TX",
        zipCode: "77005",
        preferences: "Likes hands-on work",
        skills: new[] { VolunteerSkill.Driving, VolunteerSkill.Construction },
        availability: new[] { "2025-12-04T18:00:00Z" }
    ));
    users.Add(v3);

    var v4 = new UserCredentials(
        email: "jordan.rivera@example.com",
        passwordHash: "DEV_ONLY_HASH",
        role: UserRole.Volunteer);
    v4.AttachProfile(new UserProfile(
        userCredentialsId: v4.Id,
        firstName: "Jordan",
        lastName: "Rivera",
        addressOne: "404 Pine Ave",
        city: "Houston",
        state: "TX",
        zipCode: "77006",
        preferences: "Enjoys community-facing work",
        skills: new[] { VolunteerSkill.ElderlyCare, VolunteerSkill.EventPlanning },
        availability: new[] { "2025-12-02T09:00:00Z", "2025-12-04T16:00:00Z" }
    ));
    users.Add(v4);

    var v5 = new UserCredentials(
        email: "sam.taylor@example.com",
        passwordHash: "DEV_ONLY_HASH",
        role: UserRole.Volunteer);
    v5.AttachProfile(new UserProfile(
        userCredentialsId: v5.Id,
        firstName: "Sam",
        lastName: "Taylor",
        addressOne: "505 Birch Rd",
        city: "Houston",
        state: "TX",
        zipCode: "77007",
        preferences: "Good with logistics and setup",
        skills: new[] { VolunteerSkill.LegalAid, VolunteerSkill.MedicalAid },
        availability: new[] { "2025-12-05T08:00:00Z", "2025-12-08T19:00:00Z" }
    ));
    users.Add(v5);

    var v6 = new UserCredentials(
        email: "maria.garcia@example.com",
        passwordHash: "DEV_ONLY_HASH",
        role: UserRole.Volunteer);
    v6.AttachProfile(new UserProfile(
        userCredentialsId: v6.Id,
        firstName: "Maria",
        lastName: "Garcia",
        addressOne: "606 Elm St",
        city: "Houston",
        state: "TX",
        zipCode: "77008",
        preferences: "Interested in health and safety",
        skills: new[] { VolunteerSkill.Teaching, VolunteerSkill.Photography },
        availability: new[] { "2025-12-06T10:00:00Z", "2025-12-09T16:00:00Z" }
    ));
    users.Add(v6);

    db.Users.AddRange(users);
    await db.SaveChangesAsync();

    Console.WriteLine("Seeded 8 users & profiles.");
    return users;
}

static async Task<List<Event>> SeedEventsAsync(AppDbContext db)
{
    var now = DateTime.UtcNow;

    var events = new List<Event>
    {
        // Past events (for history)
        new Event(
            name: "Community Clean-Up",
            description: "Help pick up litter and refresh the local park and creek trail.",
            location: "Central Park",
            dateUtc: now.AddDays(-30),
            urgency: EventUrgency.Medium,
            requiredSkills: new[] { VolunteerSkill.Gardening, VolunteerSkill.Construction }),

        new Event(
            name: "Food Drive Assistance",
            description: "Sort donations and assemble food boxes for local families.",
            location: "2nd Street Community Center",
            dateUtc: now.AddDays(-20),
            urgency: EventUrgency.High,
            requiredSkills: new[] { VolunteerSkill.Driving, VolunteerSkill.ElderlyCare }),

        new Event(
            name: "Elderly Care Visit",
            description: "Spend time with residents, play games, and chat.",
            location: "Sunrise Care Home",
            dateUtc: now.AddDays(-10),
            urgency: EventUrgency.Low,
            requiredSkills: new[] { VolunteerSkill.ElderlyCare }),

        new Event(
            name: "Community Garden Workday",
            description: "Weed, mulch, and plant at the neighborhood community garden.",
            location: "Greenway Community Garden",
            dateUtc: now.AddDays(-5),
            urgency: EventUrgency.Medium,
            requiredSkills: new[] { VolunteerSkill.Gardening }),

        // Upcoming events (for matching)
        new Event(
            name: "Tree Planting Day",
            description: "Plant native trees along the riverfront.",
            location: "Riverfront Trailhead",
            dateUtc: now.AddDays(5),
            urgency: EventUrgency.Medium,
            requiredSkills: new[] { VolunteerSkill.Gardening, VolunteerSkill.ChildCare}),

        new Event(
            name: "Winter Coat Drive",
            description: "Sort and distribute winter coats to unhoused individuals.",
            location: "Hope Shelter Warehouse",
            dateUtc: new DateTime(DateTime.UtcNow.Year, 12, 10, 17, 30, 0, DateTimeKind.Utc),
            urgency: EventUrgency.Medium,
            requiredSkills: new[] { VolunteerSkill.EventPlanning, VolunteerSkill.Writing }),

        new Event(
            name: "Toy Distribution Day",
            description: "Distribute donated toys to children ahead of the holidays.",
            location: "Northside Community Gym",
            dateUtc: new DateTime(DateTime.UtcNow.Year, 12, 15, 16, 0, 0, DateTimeKind.Utc),
            urgency: EventUrgency.High,
            requiredSkills: new[] { VolunteerSkill.ChildCare, VolunteerSkill.ITSupport }),

        new Event(
            name: "Senior Holiday Visits",
            description: "Spend time visiting seniors during the holiday season.",
            location: "Golden Years Living Center",
            dateUtc: new DateTime(DateTime.UtcNow.Year, 12, 20, 14, 0, 0, DateTimeKind.Utc),
            urgency: EventUrgency.Low,
            requiredSkills: new[] { VolunteerSkill.ElderlyCare }),

        new Event(
            name: "New Year Community Kickoff",
            description: "Help host a community gathering to welcome the new year.",
            location: "City Plaza",
            dateUtc: new DateTime(DateTime.UtcNow.Year + 1, 1, 4, 18, 0, 0, DateTimeKind.Utc),
            urgency: EventUrgency.Medium,
            requiredSkills: new[] { VolunteerSkill.EventPlanning, VolunteerSkill.ITSupport }),

        new Event(
            name: "After-School Tutoring",
            description: "Support students with homework and educational games.",
            location: "Westside Youth Center",
            dateUtc: now.AddDays(10),
            urgency: EventUrgency.Medium,
            requiredSkills: new[] { VolunteerSkill.ChildCare }),

        new Event(
            name: "Mobile Health Clinic Support",
            description: "Help with check-in and logistics at a mobile clinic.",
            location: "Eastside Parking Lot",
            dateUtc: now.AddDays(15),
            urgency: EventUrgency.High,
            requiredSkills: new[] { VolunteerSkill.Cooking, VolunteerSkill.AnimalCare }),

        new Event(
            name: "Disaster Relief Packing",
            description: "Prepare emergency supply kits for disaster response.",
            location: "Relief Center Warehouse",
            dateUtc: now.AddDays(20),
            urgency: EventUrgency.High,
            requiredSkills: new[] { VolunteerSkill.Photography, VolunteerSkill.Gardening })
    };

    db.Events.AddRange(events);
    await db.SaveChangesAsync();

    Console.WriteLine("Seeded 12 events.");
    return events;
}

static async Task SeedNotificationsAsync(
    AppDbContext db,
    List<UserCredentials> users)
{
    var notifications = new List<Notification>();

    foreach (var user in users)
    {
        var welcome = new Notification(
            userId: user.Id,
            message: user.Role == UserRole.Admin
                ? "Welcome, Admin! Your dashboard is ready."
                : "Thanks for signing up to volunteer!");

        notifications.Add(welcome);

        if (user.Role == UserRole.Volunteer)
        {
            notifications.Add(new Notification(
                userId: user.Id,
                message: "Reminder: You have upcoming volunteer opportunities."));
        }
    }

    db.Notifications.AddRange(notifications);
    await db.SaveChangesAsync();
    Console.WriteLine("Seeded notifications.");
}

static async Task SeedVolunteerHistoryAsync(
    AppDbContext db,
    List<UserCredentials> users,
    List<Event> events)
{
    var volunteers = users
        .Where(u => u.Role == UserRole.Volunteer)
        .ToList();

    var pastEvents = events
        .Where(e => e.DateUtc <= DateTime.UtcNow)
        .OrderBy(e => e.DateUtc)
        .ToList();

    if (!pastEvents.Any() || !volunteers.Any())
    {
        Console.WriteLine("No past events or volunteers to seed history for.");
        return;
    }

    var histories = new List<VolunteerHistory>();

    // Simple pattern: each volunteer gets history on some of the past events
    for (var i = 0; i < volunteers.Count; i++)
    {
        var volunteer = volunteers[i];
        var assignedEvents = pastEvents
            .Where((_, idx) => idx % volunteers.Count == i % volunteers.Count || idx == i % pastEvents.Count)
            .Take(3) // up to 3 events per volunteer
            .ToList();

        foreach (var ev in assignedEvents)
        {
            histories.Add(new VolunteerHistory(
                userId: volunteer.Id,
                eventId: ev.Id,
                dateUtc: ev.DateUtc,
                durationMinutes: 120 + (i * 30))); // slightly varied durations
        }
    }

    db.VolunteerHistories.AddRange(histories);
    await db.SaveChangesAsync();

    Console.WriteLine($"Seeded {histories.Count} volunteer history records.");
}
