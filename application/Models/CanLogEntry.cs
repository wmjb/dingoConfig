using domain.Models;

namespace application.Models;

public record CanLogEntry(DateTime Timestamp, string Direction, int Id, int Len, byte[] Payload) : 
    CanFrame(Id, Len, Payload);