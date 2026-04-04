using System;
using System.Collections.Generic;

namespace Garmin.Dto;

public record NativeOAuth2Session
{
	public List<DITokenSlot> TokenSlots { get; set; } = new();
	public DateTime StoredAt { get; set; }
}
