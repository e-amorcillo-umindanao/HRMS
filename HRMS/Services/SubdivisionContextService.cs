namespace HRMS.Services;

public class SubdivisionContextService
{
    private int? _selectedSubdivisionId;

    public int? SelectedSubdivisionId
    {
        get => _selectedSubdivisionId;
        private set
        {
            if (value.HasValue && value.Value <= 0)
            {
                return;
            }

            _selectedSubdivisionId = value;
        }
    }

    public string SelectedSubdivisionName { get; private set; } = string.Empty;
    public event Action? OnSubdivisionChanged;

    public void SetSubdivision(int id, string name)
    {
        if (id <= 0)
        {
            return;
        }

        SelectedSubdivisionId = id;
        SelectedSubdivisionName = name;
        OnSubdivisionChanged?.Invoke();
    }

    public void Clear()
    {
        SelectedSubdivisionId = null;
        SelectedSubdivisionName = string.Empty;
        OnSubdivisionChanged?.Invoke();
    }
}
