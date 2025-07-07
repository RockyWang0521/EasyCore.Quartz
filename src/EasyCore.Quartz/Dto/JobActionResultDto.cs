namespace EasyCore.Quartz.Dto
{
    /// <summary>
    /// Represents the result of a job-related operation.
    /// </summary>
    public class JobActionResultDto
    {
        /// <summary>
        /// The name of the job.
        /// </summary>
        public string JobName { get; set; } = default!;

        /// <summary>
        /// The group to which the job belongs. Default is "DEFAULT".
        /// </summary>
        public string Group { get; set; } = "DEFAULT";

        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// A message providing additional information about the operation result.
        /// </summary>
        public string Message { get; set; } = default!;
    }

}
