Feature: Test the Jobs Controller

  Scenario: Create a new job
  
    Given the following request
    """
    POST /api/job HTTP/1.1
    Content-type: application/json; charset=utf-8
    Accept: application/json

    {
        "JobType": "Upgrade"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-type: application/json; charset=utf-8

    {
        "jobId": [[JOBID]]
    }
    """

    Given the following request
    """
    GET /api/job/status/{{JOBID}} HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-type: application/json; charset=utf-8

    {
        "jobId": "{{JOBID}}",
        "status": "Pending"
    }
    """
  
  
  Scenario: Invalid input for Create Job
  
    Given the following request
    """
    POST /api/job HTTP/1.1
    Content-type: application/json; charset=utf-8
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-type: application/problem+json; charset=utf-8

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "One or more validation errors occurred.",
      "status": 400,
      "errors": {
        "request": ["The request field is required."]
      }
    }
    """

  Scenario: Invalid Job Type for Create Job
  
    Given the following request
    """
    POST /api/job HTTP/1.1
    Content-type: application/json; charset=utf-8
    Accept: application/json

    {
        "JobType": ""
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-type: application/problem+json; charset=utf-8

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      "title": "Job Type Invalid",
      "status": 400,
      "detail": "The Job Type '' is invalid."
    }
    """


  Scenario: Get the status for a job that doesn't exist

    Given the following request
    """
    GET /api/job/status/_DOES_NOT_EXIST_ HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 404 NotFound
    Content-type: application/problem+json; charset=utf-8

    {
      "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
      "title": "Job Not Found",
      "detail": "The job with ID _DOES_NOT_EXIST_ was not found.",
      "status": 404
    }
    """
