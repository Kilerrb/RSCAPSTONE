using System;
using System.Collections.Generic;
using System.Linq;

public enum Rank
{
    User,
    Manager,
    Admin
}

public class User
{
    public string Username { get; }
    private string Password { get; }
    public List<Reservation> Reservations { get; }
    public Rank UserRank { get; }

    public User(string username, string password, Rank rank)
    {
        Username = username;
        Password = password;
        Reservations = new List<Reservation>();
        UserRank = rank;
    }

    public bool Authenticate(string password)
    {
        return Password == password;
    }

    public virtual void MakeReservation(Room room, DateTime startTime, DateTime endTime)
    {
        if (!IsAvailable(room, startTime, endTime))
        {
            Console.WriteLine($"Failed to make reservation. Room {room.RoomNumber} is not available during the requested time.");
            return;
        }

        Reservation newReservation = new Reservation(this, room, startTime, endTime);
        Reservations.Add(newReservation);
        room.AddReservation(newReservation);
        Console.WriteLine($"Reservation made by {Username} for Room {room.RoomNumber} from {startTime} to {endTime}");
        PrintReservationDetails(newReservation);
    }

    private bool IsAvailable(Room room, DateTime startTime, DateTime endTime)
    {
        foreach (var reservation in room.Reservations)
        {
            if (startTime < reservation.EndTime && endTime > reservation.StartTime)
            {
                return false;
            }
        }
        return true;
    }

    public virtual void CancelReservation(Reservation reservation)
    {
        if (Reservations.Contains(reservation))
        {
            Reservations.Remove(reservation);
            reservation.Room.RemoveReservation(reservation);
            Console.WriteLine($"Reservation for Room {reservation.Room.RoomNumber} from {reservation.StartTime} to {reservation.EndTime} cancelled by {Username}");
        }
        else
        {
            Console.WriteLine($"Cannot cancel reservation. This reservation does not belong to {Username}");
        }
    }

    private void PrintReservationDetails(Reservation reservation)
    {
        Console.WriteLine($"Reservation Details:");
        Console.WriteLine($"Room: {reservation.Room.RoomNumber}");
        Console.WriteLine($"Start Time: {reservation.StartTime}");
        Console.WriteLine($"End Time: {reservation.EndTime}");
        Console.WriteLine($"Reserved By: {Username}");
    }

    public void ScanQRCode()
    {
        Console.WriteLine($"{Username} scanned a QR code to join a room.");
    }
}

public class Manager : User
{
    public Manager(string username, string password) : base(username, password, Rank.Manager)
    {
    }

    public void AddRoom(Room room, Admin admin)
    {
        admin.AddRoom(room);
    }

    public void RemoveRoom(Room room, Admin admin)
    {
        admin.RemoveRoom(room);
    }

    public void AddSchedule(Room room, RoomSchedule schedule)
    {
        room.AddSchedule(schedule);
    }
}

public class Admin : User
{
    public List<Room> AvailableRooms { get; }

    public Admin(string username, string password) : base(username, password, Rank.Admin)
    {
        AvailableRooms = new List<Room>();
    }

    public void AddRoom(Room room)
    {
        AvailableRooms.Add(room);
        Console.WriteLine($"Room {room.RoomNumber} has been added.");
        LogAction($"Room {room.RoomNumber} added by Admin {Username}");
    }

    public void RemoveRoom(Room room)
    {
        if (AvailableRooms.Contains(room))
        {
            AvailableRooms.Remove(room);
            Console.WriteLine($"Room {room.RoomNumber} has been removed.");
            LogAction($"Room {room.RoomNumber} removed by Admin {Username}");
        }
        else
        {
            Console.WriteLine($"Room {room.RoomNumber} is not available for removal.");
        }
    }
    public void ViewLogs()
    {
        Console.WriteLine("Logs:");
        foreach (var log in ReservationSystem.Logs)
        {
            Console.WriteLine(log);
        }
    }

    private void LogAction(string action)
    {
        ReservationSystem.Logs.Add(action);
    }
}

public class Room
{
    public string RoomNumber { get; }
    public List<RoomSchedule> Schedules { get; }
    public List<Reservation> Reservations { get; }

    public Room(string roomNumber)
    {
        RoomNumber = roomNumber;
        Schedules = new List<RoomSchedule>();
        Reservations = new List<Reservation>();
    }

    public void AddSchedule(RoomSchedule schedule)
    {
        Schedules.Add(schedule);
        Console.WriteLine($"Schedule added to Room {RoomNumber} from {schedule.StartTime} to {schedule.EndTime}");
    }

    public void AddReservation(Reservation reservation)
    {
        Reservations.Add(reservation);
    }

    public void RemoveReservation(Reservation reservation)
    {
        Reservations.Remove(reservation);
    }
}

public class RoomSchedule
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }

    public RoomSchedule(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
}

public class Reservation
{
    public User User { get; }
    public Room Room { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }

    public Reservation(User user, Room room, DateTime startTime, DateTime endTime)
    {
        User = user;
        Room = room;
        StartTime = startTime;
        EndTime = endTime;
    }
}

public class ReservationSystem
{
    public static List<string> Logs = new List<string>();
    private Dictionary<string, User> users;

    public ReservationSystem()
    {
        users = new Dictionary<string, User>();
    }

    public void SignUp(string username, string password, Rank rank)
    {
        if (!users.ContainsKey(username))
        {
            User newUser;
            if (rank == Rank.Admin)
            {
                newUser = new Admin(username, password);
            }
            else if (rank == Rank.Manager)
            {
                newUser = new Manager(username, password);
            }
            else
            {
                newUser = new User(username, password, rank);
            }

            users.Add(username, newUser);
            Console.WriteLine($"{rank} {username} signed up successfully.");
            Logs.Add($"{rank} {username} signed up successfully.");
        }
        else
        {
            Console.WriteLine($"Username {username} is already taken.");
        }
    }

    public User Login(string username, string password)
    {
        if (users.TryGetValue(username, out User user))
        {
            if (user.Authenticate(password))
            {
                Console.WriteLine($"{username} logged in successfully.");
                Logs.Add($"{username} logged in successfully.");
                return user;
            }
            else
            {
                Console.WriteLine("Invalid password.");
                return null;
            }
        }
        else
        {
            Console.WriteLine("User not found.");
            return null;
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        ReservationSystem system = new ReservationSystem();

        system.SignUp("user1", "password123", Rank.User);
        system.SignUp("admin1", "adminpassword", Rank.Admin);
        system.SignUp("manager1", "managerpassword", Rank.Manager);

        User user1 = system.Login("user1", "password123");
        Admin admin1 = (Admin)system.Login("admin1", "adminpassword");
        Manager manager1 = (Manager)system.Login("manager1", "managerpassword");

        if (admin1 != null)
        {
            Room room101 = new Room("101");
            Room room102 = new Room("102");

            manager1.AddRoom(room101, admin1);
            manager1.AddRoom(room102, admin1);
            manager1.AddSchedule(room101, new RoomSchedule(DateTime.Parse("2024-05-01 08:00"), DateTime.Parse("2024-05-01 12:00")));
            manager1.AddSchedule(room101, new RoomSchedule(DateTime.Parse("2024-05-01 13:00"), DateTime.Parse("2024-05-01 17:00")));
            manager1.AddSchedule(room102, new RoomSchedule(DateTime.Parse("2024-05-01 09:00"), DateTime.Parse("2024-05-01 11:00")));

            if (user1 != null)
            {
                user1.MakeReservation(room101, DateTime.Parse("2024-05-01 09:00"), DateTime.Parse("2024-05-01 11:00"));
                user1.MakeReservation(room102, DateTime.Parse("2024-05-01 10:00"), DateTime.Parse("2024-05-01 12:00"));

                user1.CancelReservation(user1.Reservations[0]);

                user1.ScanQRCode();
            }

            admin1.ViewLogs();
        }

        Console.ReadKey();
    }
}
