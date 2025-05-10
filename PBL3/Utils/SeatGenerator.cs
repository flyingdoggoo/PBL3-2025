using System;
using System.Collections.Generic;
using PBL3.Models;
using PBL3.Models.ViewModels;

namespace PBL3.Utils
{
    public static class SeatGenerator
    {
        // The method is now public so it can be called from other classes
        public static int GenerateSeatsForSection(List<Seat> allSeatsList, Section section, int startingRow)
        {
            if (section == null || section.SectionId <= 0 || section.Capacity <= 0)
            {
                // It's good practice to return the startingRow unmodified if nothing is done,
                // or throw an ArgumentException if the input is invalid for generation.
                // For now, let's stick to your original logic of returning 1,
                // but returning 'startingRow' might be more consistent.
                // Let's return 'startingRow' to indicate no rows were consumed.
                return startingRow;
            }

            string[] columns;
            int seatsPerRow;

            if (section.SectionName != null && section.SectionName.Contains("Thương gia", StringComparison.OrdinalIgnoreCase))
            {
                columns = new[] { "A", "C", "D", "F" }; // Example 2-2 (aisles B, E)
                seatsPerRow = 4;
            }
            else // Economy or other
            {
                columns = new[] { "A", "B", "C", "D", "E", "F" }; // Example 3-3
                seatsPerRow = 6;
            }

            // Ensure seatsPerRow is not zero to avoid division by zero
            if (seatsPerRow <= 0)
            {
                // Handle error: perhaps log it or throw an exception,
                // or default to a safe value if appropriate for your domain.
                // For now, let's assume it won't be zero based on the logic above.
                // If it could be, add a check here.
                return startingRow; // Or throw
            }

            int totalRowsInSection = (int)Math.Ceiling((double)section.Capacity / seatsPerRow);
            int seatsGeneratedForSection = 0;

            // The 'startingRowNumber' local variable was not actually used in your original loop logic.
            // The loop's progression is correctly handled by 'row' being initialized with 'startingRow'
            // and incrementing up to 'startingRow + totalRowsInSection'.
            // int startingRowNumber = 1; // This line was effectively unused.

            int currentRow = startingRow; // Use a different variable name for clarity in the loop
            for (; currentRow < startingRow + totalRowsInSection && seatsGeneratedForSection < section.Capacity; currentRow++)
            {
                foreach (string col in columns)
                {
                    if (seatsGeneratedForSection >= section.Capacity) break;

                    allSeatsList.Add(new Seat
                    {
                        SectionId = section.SectionId,
                        SeatNumber = $"{currentRow}{col}",
                        Row = currentRow,
                        Column = col,
                        Status = "Available"
                        // TicketId will be null by default for a nullable type
                    });
                    seatsGeneratedForSection++;
                }
            }
            // Return the next row number to start generating seats for the next section
            return currentRow;
        }
    }
}
