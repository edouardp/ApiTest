Feature: Add Two Numbers

  As a user of the Math API
  I want to send two numbers as parameters
  So that I can receive their sum in the response

  Scenario: Add two positive integers

    Given the following request
    """
    GET /api/math/add?a=5&b=6 HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-type: application/json; charset=utf-8

    11
    """
  

  Scenario: Add two negative integers

    Given the following request
    """
    GET /api/math/add?a=-1&b=-2 HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-type: application/json; charset=utf-8

    -3
    """
      

  Scenario: Add two floating point numbers

    Given the following request
    """
    GET /api/math/add?a=1.5&b=2.0 HTTP/1.1
    Accept: application/json
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-type: application/json; charset=utf-8

    3.5
    """

