using System;

namespace Seed.Process.Transfer.Model
{
    class TRF_Course : BaseDto
    {

        public Guid Id { get; set; }
        public string Name { get; set; }

        public string Cover { get; set; }

        public int CourseOrder { get; set; }

        public string Locale { get; set; }


    }
}
