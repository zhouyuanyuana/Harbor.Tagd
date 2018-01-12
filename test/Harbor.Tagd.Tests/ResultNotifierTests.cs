﻿using AutoFixture;
using AutoFixture.Xunit2;
using Harbor.Tagd.Rules;
using Harbormaster.Models;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Harbor.Tagd.Tests
{
	public class ResultNotifierTests : TagEngineTest
	{
		[Theory, AutoData]
		public async Task PostsResults(List<Project> projects, List<Repository> repos)
		{
			Harbor.Setup(h => h.GetAllProjects(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>())).ReturnsAsync(projects);
			Harbor.Setup(h => h.GetRepositories(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(repos);

			var tags = _fixture.CreateMany<Tag>(10);
			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(tags);

			ProcessResult result = null;
			NotificationHandler.Setup(n => n.Notify(It.IsAny<ProcessResult>())).Callback<ProcessResult>(c => result = c);

			Rules.Setup(r => r.Load()).Returns(new RuleSet
			{
				IgnoreGlobally = new RuleSet.GlobalIgnoreSettings
				{
					Projects = new[] { projects.First().Name },
					Repos = new[] { repos.First().Name },
				},
				DefaultRule = new Rule
				{
					Project = new Regex(".*"),
					Repo = new Regex(".*"),
					Tag = new Regex(".*"),
					Keep = 5
				}
			}.EnsureDefaults());

			await _sut.Process();

			Assert.NotNull(result);
			Assert.Equal(new ProcessResult(removedTags: 5, ignoredTags: 5, ignoredRepos: projects.Count - 1, ignoredProjects: 1), result);
		}
	}
}