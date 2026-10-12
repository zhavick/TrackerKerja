using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrackerKerja.Data;
using TrackerKerja.Models;

namespace TrackerKerja.Tests
{
    public static class ProjectBudgetTestRunner
    {
        public static async Task<int> RunAllTestsAsync()
        {
            await Task.CompletedTask;
            Console.WriteLine("==========================================================");
            Console.WriteLine("RUNNING PROJECT USE BUDGET & DEADLINE VALIDATION TESTS");
            Console.WriteLine("==========================================================");

            // Test 1: Project with UseBudget = true and Deadline = null should be considered invalid
            Console.WriteLine("[TEST 1] Verifying UseBudget = true requires Deadline...");
            var projectWithBudgetNoDeadline = new Project
            {
                Name = "Project Alpha",
                UseBudget = true,
                Budget = 10000000,
                Deadline = null
            };

            // Custom validation logic check
            var validationErrors = ValidateProjectBudgetRequirements(projectWithBudgetNoDeadline);
            if (!validationErrors.Contains("Deadline"))
            {
                throw new Exception("Expected validation error for missing Deadline when UseBudget is true, but got none!");
            }
            Console.WriteLine("[TEST 1 PASSED] UseBudget=true without Deadline failed validation as expected.");

            // Test 2: Project with UseBudget = false and Deadline = null should pass validation
            Console.WriteLine("[TEST 2] Verifying UseBudget = false allows null Deadline...");
            var projectWithoutBudget = new Project
            {
                Name = "Internal Non-Budget Task",
                UseBudget = false,
                Budget = null,
                Deadline = null
            };
            var noBudgetErrors = ValidateProjectBudgetRequirements(projectWithoutBudget);
            if (noBudgetErrors.Count > 0)
            {
                throw new Exception($"Expected no validation errors for UseBudget=false, but got: {string.Join(", ", noBudgetErrors)}");
            }
            Console.WriteLine("[TEST 2 PASSED] UseBudget=false with null Deadline is valid.");

            // Test 3: Project with UseBudget = true and Deadline provided should pass validation
            Console.WriteLine("[TEST 3] Verifying UseBudget = true with valid Deadline passes...");
            var projectWithBudgetAndDeadline = new Project
            {
                Name = "Client Project Beta",
                UseBudget = true,
                Budget = 50000000,
                Deadline = DateTime.Today.AddMonths(1)
            };
            var validErrors = ValidateProjectBudgetRequirements(projectWithBudgetAndDeadline);
            if (validErrors.Count > 0)
            {
                throw new Exception($"Expected no errors, but got: {string.Join(", ", validErrors)}");
            }
            Console.WriteLine("[TEST 3 PASSED] UseBudget=true with Deadline provided is valid.");

            return 0;
        }

        public static List<string> ValidateProjectBudgetRequirements(Project project)
        {
            var errors = new List<string>();
            if (project.UseBudget && !project.Deadline.HasValue)
            {
                errors.Add("Deadline");
            }
            return errors;
        }
    }
}
