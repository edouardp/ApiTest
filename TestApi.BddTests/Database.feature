Feature: Database Integration
    As a developer
    I want to test database operations
    So that I can ensure data persistence works correctly

Background:
    Given the database contains a job with title "Software Engineer"
    And the database contains a user with name "Test User"

Scenario: Query all jobs from database
    When I query the database for jobs
    Then the queried jobs should contain a job with title "Software Engineer"
    And the database should contain 4 jobs

Scenario: Query all users from database
    When I query the database for users
    Then the queried users should contain a user with name "Test User"
    And the database should contain 5 users

Scenario: Delete a job from database
    Given the database contains a job with title "Temporary Job"
    When I delete the job with title "Temporary Job"
    Then 1 job should be deleted

Scenario: Verify initial database state
    Then the database should contain 4 jobs
    And the database should contain 5 users
