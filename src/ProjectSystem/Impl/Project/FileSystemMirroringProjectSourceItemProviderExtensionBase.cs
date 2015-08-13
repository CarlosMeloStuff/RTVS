﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Items;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Threading;
using ItemData = System.Tuple<string, string, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>>;

namespace Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.Project
{
	public abstract class FileSystemMirroringProjectSourceItemProviderExtensionBase : IProjectSourceItemProviderExtension,
		IProjectFolderItemProviderExtension
	{
		private readonly UnconfiguredProject _unconfiguredProject;
		private readonly ConfiguredProject _configuredProject;
		private readonly IProjectLockService _projectLockService;

		protected FileSystemMirroringProjectSourceItemProviderExtensionBase(UnconfiguredProject unconfiguredProject, ConfiguredProject configuredProject, IProjectLockService projectLockService)
		{
			_unconfiguredProject = unconfiguredProject;
			_configuredProject = configuredProject;
			_projectLockService = projectLockService;
		}

		#region IProjectSourceItemProviderExtension implementation

		public Task<bool> CheckSourceItemOwnershipAsync(string itemType, string evaluatedInclude)
		{
			return this.CheckFolderItemOwnershipAsync(evaluatedInclude);
		}

		public Task<bool> CheckProjectFileOwnershipAsync(string projectFilePath)
		{
			return this.CheckProjectFileOwnership(projectFilePath)
				? TplExtensions.TrueTask
				: TplExtensions.FalseTask;
		}

		public Task<IReadOnlyCollection<ItemData>> AddOwnedSourceItemsAsync(IReadOnlyCollection<ItemData> items)
		{
			var projectDirectory = _unconfiguredProject.GetProjectDirectory();
			var unhandledItemData =
				items.Where(
					item => PathHelper.IsOutsideProjectDirectory(projectDirectory, _unconfiguredProject.MakeRooted(item.Item2)))
					.ToImmutableArray();

			return Task.FromResult<IReadOnlyCollection<ItemData>>(unhandledItemData);
		}

		public Task<bool> TryAddSourceItemsToOwnedProjectFileAsync(IReadOnlyCollection<ItemData> items, string projectFilePath)
		{
			return Task.FromResult(CheckProjectFileOwnership(projectFilePath));
		}

		public Task<IReadOnlyCollection<IProjectSourceItem>> RemoveOwnedSourceItemsAsync(
			IReadOnlyCollection<IProjectSourceItem> projectItems, DeleteOptions deleteOptions)
		{
			var projectDirectory = _unconfiguredProject.GetProjectDirectory();
			List<IProjectSourceItem> itemsInProjectFolder = projectItems
				.Where(item => !PathHelper.IsOutsideProjectDirectory(projectDirectory, item.EvaluatedIncludeAsFullPath))
				.ToList();

			return
				Task.FromResult(itemsInProjectFolder.Count == 0
					? projectItems
					: projectItems.Except(itemsInProjectFolder).ToImmutableArray());
		}

		public Task<ProjectItem> RenameOwnedSourceItemAsync(IProjectItem projectItem, string newValue)
		{
			return GetMsBuildItemByProjectItem(projectItem);
		}

		public Task<ProjectItem> SetItemTypeOfOwnedSourceItemAsync(IProjectItem projectItem, string newItemType)
		{
			return GetMsBuildItemByProjectItem(projectItem);
		}

		#endregion

		#region IProjectFolderItemProviderExtension implementation

		public Task<bool> CheckFolderItemOwnershipAsync(string evaluatedInclude)
		{
			return _unconfiguredProject.IsOutsideProjectDirectory(_unconfiguredProject.MakeRooted(evaluatedInclude))
				? TplExtensions.FalseTask
				: TplExtensions.TrueTask;
		}

		public Task<IReadOnlyDictionary<string, IEnumerable<KeyValuePair<string, string>>>> AddOwnedFolderItemsAsync(
			IReadOnlyDictionary<string, IEnumerable<KeyValuePair<string, string>>> items)
		{
			var projectDirectory = _unconfiguredProject.GetProjectDirectory();
			var unhandledItemData =
				items.Where(
					item => PathHelper.IsOutsideProjectDirectory(projectDirectory, _unconfiguredProject.MakeRooted(item.Key)))
					.ToImmutableDictionary();

			return Task.FromResult<IReadOnlyDictionary<string, IEnumerable<KeyValuePair<string, string>>>>(unhandledItemData);
		}

		public Task<IReadOnlyCollection<IProjectItem>> RemoveOwnedFolderItemsAsync(
			IReadOnlyCollection<IProjectItem> projectItems, DeleteOptions deleteOptions)
		{
			List<IProjectItem> itemsInProjectFolder = projectItems
				.Where(item => !_unconfiguredProject.IsOutsideProjectDirectory(item.EvaluatedIncludeAsFullPath))
				.ToList();

			return
				Task.FromResult(itemsInProjectFolder.Count == 0
					? projectItems
					: projectItems.Except(itemsInProjectFolder).ToImmutableArray());
		}

		public Task<ProjectItem> RenameOwnedFolderItemAsync(IProjectItem projectItem, string newValue)
		{
			return GetMsBuildItemByProjectItem(projectItem);
		}

		#endregion

		private bool CheckProjectFileOwnership(string projectFilePath)
		{
			return _unconfiguredProject.GetInMemoryTargetsFileFullPath().Equals(projectFilePath, StringComparison.OrdinalIgnoreCase);
		}

		private async Task<ProjectItem> GetMsBuildItemByProjectItem(IProjectItem projectItem)
		{
			using (var access = await _projectLockService.ReadLockAsync())
			{
				var project = await access.GetProjectAsync(_configuredProject);
				return project.GetItemsByEvaluatedInclude(projectItem.EvaluatedInclude).FirstOrDefault(pi => StringComparer.OrdinalIgnoreCase.Equals(pi.ItemType, projectItem.ItemType));
			}
		}
	}
}