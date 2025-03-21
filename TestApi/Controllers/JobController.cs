using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace TestApi.Controllers;

[ApiController]
[Route("api/job")]
public class JobController : ControllerBase
{
    private readonly IJobStore jobStore;

    public JobController(IJobStore jobStore)
    {
        this.jobStore = jobStore;
    }

    [HttpPost]
    public IActionResult CreateJob([FromBody] JobRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.JobType))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Job Type Invalid",
                detail: $"The Job Type '{request.JobType}' is invalid."
            );
        }

        var jobId = Guid.NewGuid().ToString();
        jobStore.AddJob(jobId, "Pending");

        var response = new JobResponse(jobId);
        return CreatedAtAction(nameof(GetJobStatus), new { jobId }, response);
    }

    [HttpGet("status/{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        if (!jobStore.TryGetJobStatus(jobId, out var status))
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Job Not Found",
                detail: $"The job with ID {jobId} was not found."
            );
        }

        var response = new JobStatusResponse(jobId, status);
        return Ok(response);
    }
}


public record JobRequest(string JobType);
public record JobResponse(string JobId);
public record JobStatusResponse(string JobId, string Status);

public interface IJobStore
{
    void AddJob(string jobId, string status);
    bool TryGetJobStatus(string jobId, out string status);
}

public class JobStore : IJobStore
{
    private readonly ConcurrentDictionary<string, string> jobs = new();

    public void AddJob(string jobId, string status)
    {
        jobs[jobId] = status;
    }

    public bool TryGetJobStatus(string jobId, out string status)
    {
        return jobs.TryGetValue(jobId, out status);
    }
}
