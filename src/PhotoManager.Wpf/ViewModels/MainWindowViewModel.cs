using System.Collections.ObjectModel;
using System.IO;
using PhotoManager.Application.Abstractions;
using PhotoManager.Application.Models;
using PhotoManager.Wpf.Services;

namespace PhotoManager.Wpf.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IPhotoCatalog _catalog;
    private readonly IPhotoScanner _scanner;
    private readonly IThumbnailService _thumbnails;
    private readonly IDialogService _dialogs;
    private readonly HashSet<long> _selectedIds = [];
    private CancellationTokenSource? _scanCancellation;
    private CancellationTokenSource? _searchCancellation;
    private CancellationTokenSource? _tagSuggestionsCancellation;
    private string _search = ""; private string _tagFilter = ""; private string _tagInput = ""; private string _notes = "";
    private int _minimumRating; private int _selectedRating; private int _totalCount; private bool _isBusy; private double _progressValue;
    private string _status = ""; private PhotoItemViewModel? _selectedPhoto;

    public ObservableCollection<PhotoItemViewModel> Photos { get; } = [];
    public ObservableCollection<string> SelectedTags { get; } = [];
    public ObservableCollection<string> SuggestedTags { get; } = [];
    public Localizer L { get; } = new();
    public IReadOnlyList<int> Ratings { get; } = [0, 1, 2, 3, 4, 5];
    public string Search { get => _search; set { if (Set(ref _search, value)) DebounceReload(); } }
    public string TagFilter { get => _tagFilter; set { if (Set(ref _tagFilter, value)) DebounceReload(); } }
    public string TagInput { get => _tagInput; set { if (Set(ref _tagInput, value)) { RaiseTagCommands(); DebounceTagSuggestions(); } } }
    public string Notes { get => _notes; set => Set(ref _notes, value); }
    public int MinimumRating { get => _minimumRating; set { if (Set(ref _minimumRating, value)) DebounceReload(); } }
    public int SelectedRating { get => _selectedRating; set { if (Set(ref _selectedRating, value)) _ = SetRatingAsync(value); } }
    public int TotalCount { get => _totalCount; private set => Set(ref _totalCount, value); }
    public bool IsBusy { get => _isBusy; private set { if (Set(ref _isBusy, value)) { ScanCommand.Raise(); CancelCommand.Raise(); LoadMoreCommand.Raise(); } } }
    public double ProgressValue { get => _progressValue; private set => Set(ref _progressValue, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public PhotoItemViewModel? SelectedPhoto { get => _selectedPhoto; private set => Set(ref _selectedPhoto, value); }

    public AsyncCommand ScanCommand { get; }
    public RelayCommand CancelCommand { get; }
    public AsyncCommand LoadMoreCommand { get; }
    public AsyncCommand AddTagsCommand { get; }
    public AsyncCommand RemoveTagCommand { get; }
    public AsyncRelayCommand<string> AddSuggestedTagCommand { get; }
    public AsyncRelayCommand<string> RemoveSelectedTagCommand { get; }
    public AsyncCommand SaveNotesCommand { get; }
    public AsyncCommand BackupCommand { get; }
    public AsyncCommand CheckMissingCommand { get; }
    public RelayCommand ToggleLanguageCommand { get; }

    public MainWindowViewModel(IPhotoCatalog catalog, IPhotoScanner scanner, IThumbnailService thumbnails, IDialogService? dialogs = null)
    {
        _catalog = catalog; _scanner = scanner; _thumbnails = thumbnails; _dialogs = dialogs ?? new DialogService();
        ScanCommand = new(ScanAsync, () => !IsBusy);
        CancelCommand = new(() => _scanCancellation?.Cancel(), () => IsBusy);
        LoadMoreCommand = new(LoadMoreAsync, () => Photos.Count < TotalCount && !IsBusy);
        AddTagsCommand = new(AddTagsAsync, () => _selectedIds.Count > 0 && ParseTags(TagInput).Count > 0);
        RemoveTagCommand = new(RemoveTypedTagAsync, () => _selectedIds.Count > 0 && ParseTags(TagInput).Count > 0);
        AddSuggestedTagCommand = new(AddSuggestedTagAsync, tag => _selectedIds.Count > 0 && !string.IsNullOrWhiteSpace(tag));
        RemoveSelectedTagCommand = new(RemoveSelectedTagAsync, tag => _selectedIds.Count > 0 && !string.IsNullOrWhiteSpace(tag));
        SaveNotesCommand = new(SaveNotesAsync, () => SelectedPhoto is not null);
        BackupCommand = new(BackupAsync, () => !IsBusy);
        CheckMissingCommand = new(CheckMissingAsync, () => !IsBusy);
        ToggleLanguageCommand = new(() => { L.Persian = !L.Persian; Raise(nameof(L)); });
    }

    public async Task LoadFirstPageAsync(CancellationToken cancellationToken = default)
    {
        var query = CurrentQuery(0);
        var rowsTask = _catalog.QueryAsync(query, cancellationToken);
        var countTask = _catalog.CountAsync(query, cancellationToken);
        var rows = await rowsTask; TotalCount = await countTask;
        Photos.Clear(); foreach (var photo in rows) Photos.Add(new(photo, _thumbnails));
        Status = $"{TotalCount:N0} {L.Photos}";
        LoadMoreCommand.Raise();
    }

    private async Task LoadMoreAsync()
    {
        var rows = await _catalog.QueryAsync(CurrentQuery(Photos.Count));
        foreach (var photo in rows) Photos.Add(new(photo, _thumbnails));
        LoadMoreCommand.Raise();
    }

    public async Task SetSelectionAsync(IEnumerable<PhotoItemViewModel> selection)
    {
        var selected = selection.ToArray();
        _selectedIds.Clear(); foreach (var item in selected) _selectedIds.Add(item.Id);
        SelectedPhoto = selected.FirstOrDefault();
        if (SelectedPhoto is not null)
        {
            Notes = SelectedPhoto.Model.Notes; _selectedRating = SelectedPhoto.Model.Rating; Raise(nameof(SelectedRating));
            TagInput = "";
            await ReloadSelectedTagsAsync();
        }
        else
        {
            Notes = ""; _selectedRating = 0; Raise(nameof(SelectedRating)); TagInput = ""; SelectedTags.Clear();
        }
        await RefreshTagSuggestionsAsync();
        RaiseTagCommands(); SaveNotesCommand.Raise();
    }

    private async Task ScanAsync()
    {
        var folder = _dialogs.ChooseFolder(L.SelectFolder); if (string.IsNullOrWhiteSpace(folder)) return;
        _scanCancellation = new(); IsBusy = true; ProgressValue = 0;
        var progress = new Progress<ScanProgress>(p =>
        {
            ProgressValue = p.Discovered == 0 ? 0 : Math.Min(100, p.Processed * 100d / p.Discovered);
            Status = $"{L.Discovered}: {p.Discovered:N0}  ·  {L.Processed}: {p.Processed:N0}  ·  {L.Added}: {p.Added:N0}  ·  {L.Failed}: {p.Failed:N0}";
        });
        try
        {
            var result = await _scanner.ScanAsync(folder, true, progress, _scanCancellation.Token);
            Status = result.Cancelled ? L.Cancel : $"{L.Complete}: {result.Added:N0}";
            await LoadFirstPageAsync();
            if (result.Errors.Count > 0)
            {
                var log = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mooli-web", "PhotoManager", "last-scan-errors.txt");
                await File.WriteAllLinesAsync(log, result.Errors);
            }
        }
        catch (Exception ex) { _dialogs.Error(L.Failed, ex.Message); }
        finally { IsBusy = false; _scanCancellation.Dispose(); _scanCancellation = null; }
    }

    private async Task AddTagsAsync()
    {
        await AddTagsToSelectionAsync(ParseTags(TagInput), clearInput: true);
    }

    private async Task AddSuggestedTagAsync(string tag)
    {
        await AddTagsToSelectionAsync([tag], clearInput: true);
    }

    private async Task AddTagsToSelectionAsync(IReadOnlyCollection<string> tags, bool clearInput)
    {
        var clean = CleanTags(tags).ToArray();
        if (_selectedIds.Count == 0 || clean.Length == 0) return;
        var photoIds = _selectedIds.ToArray();
        await _catalog.AddTagsAsync(photoIds, clean);
        if (clearInput) TagInput = "";
        await ReloadSelectedTagsAsync();
        await RefreshTagSuggestionsAsync();
        RaiseTagCommands();
    }

    private async Task RemoveTypedTagAsync()
    {
        var tags = CleanTags(ParseTags(TagInput)).ToArray();
        if (_selectedIds.Count == 0 || tags.Length == 0) return;
        var photoIds = _selectedIds.ToArray();
        foreach (var tag in tags) await _catalog.RemoveTagAsync(photoIds, tag);
        TagInput = "";
        await ReloadSelectedTagsAsync();
        await RefreshTagSuggestionsAsync();
        RaiseTagCommands();
    }

    private async Task RemoveSelectedTagAsync(string tag)
    {
        if (_selectedIds.Count == 0 || string.IsNullOrWhiteSpace(tag)) return;
        var photoIds = _selectedIds.ToArray();
        await _catalog.RemoveTagAsync(photoIds, tag.Trim());
        await ReloadSelectedTagsAsync();
        await RefreshTagSuggestionsAsync();
        RaiseTagCommands();
    }

    private async Task SetRatingAsync(int rating) { if (_selectedIds.Count > 0) await _catalog.SetRatingAsync(_selectedIds.ToArray(), rating); }
    private async Task SaveNotesAsync() { if (SelectedPhoto is not null) await _catalog.SetNotesAsync(SelectedPhoto.Id, Notes); }
    private async Task BackupAsync() { var path = _dialogs.SaveCatalog(L.Backup); if (path is not null) await _catalog.BackupAsync(path); }
    private async Task CheckMissingAsync() { IsBusy = true; try { var count = await _catalog.MarkMissingAsync(); Status = $"{count:N0}"; await LoadFirstPageAsync(); } finally { IsBusy = false; } }

    private async Task ReloadSelectedTagsAsync(CancellationToken cancellationToken = default)
    {
        SelectedTags.Clear();
        if (SelectedPhoto is null) return;
        var tags = await _catalog.GetTagsAsync(SelectedPhoto.Id, cancellationToken);
        SelectedTags.Clear();
        foreach (var tag in tags) SelectedTags.Add(tag);
    }

    private async Task RefreshTagSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        var currentTags = new HashSet<string>(SelectedTags, StringComparer.OrdinalIgnoreCase);
        var suggestions = await _catalog.SuggestTagsAsync(TagInput.Trim(), 30, cancellationToken);
        SuggestedTags.Clear();
        foreach (var tag in suggestions.Where(tag => !currentTags.Contains(tag))) SuggestedTags.Add(tag);
    }

    private async void DebounceTagSuggestions()
    {
        _tagSuggestionsCancellation?.Cancel(); _tagSuggestionsCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _tagSuggestionsCancellation = cancellation;
        try { await Task.Delay(150, cancellation.Token); await RefreshTagSuggestionsAsync(cancellation.Token); }
        catch (OperationCanceledException) { }
    }

    private void RaiseTagCommands()
    {
        AddTagsCommand.Raise(); RemoveTagCommand.Raise(); AddSuggestedTagCommand.Raise(); RemoveSelectedTagCommand.Raise();
    }

    private static IReadOnlyList<string> ParseTags(string value) =>
        value.Split([',', '،', ';', '\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static IEnumerable<string> CleanTags(IEnumerable<string> tags) =>
        tags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);

    private PhotoQuery CurrentQuery(int offset) => new(Search.Trim(), TagFilter.Trim(), MinimumRating, offset, 200);
    private async void DebounceReload()
    {
        _searchCancellation?.Cancel(); _searchCancellation?.Dispose(); _searchCancellation = new();
        try { await Task.Delay(300, _searchCancellation.Token); await LoadFirstPageAsync(_searchCancellation.Token); }
        catch (OperationCanceledException) { }
    }
}
