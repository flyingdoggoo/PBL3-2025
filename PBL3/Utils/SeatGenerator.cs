using System;
using System.Collections.Generic;
using PBL3.Models;
using PBL3.Models.ViewModels;

namespace PBL3.Utils
{
    public static class SeatGenerator
    {
        public static int GenerateSeatsForSection(List<Seat> allSeatsList, Section section, int startingRow)
        {
            if (section == null || section.SectionId <= 0 || section.Capacity <= 0)
            {
                return startingRow;
            }

            string[] columns;
            int seatsPerRow;

            if (section.SectionName != null && section.SectionName.Contains("Thương gia", StringComparison.OrdinalIgnoreCase))
            {
                columns = [ "A", "C", "D", "F" ];
                seatsPerRow = 4;
            }
            else
            {
                columns = ["A", "B", "C", "D", "E", "F" ];
                seatsPerRow = 6;
            }
            if (seatsPerRow <= 0)
            {
                return startingRow;
            }

            int totalRowsInSection = (int)Math.Ceiling((double)section.Capacity / seatsPerRow);
            int seatsGeneratedForSection = 0;

            int currentRow = startingRow;
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
                    });
                    seatsGeneratedForSection++;
                }
            }
            return currentRow;
        }
    }
}
