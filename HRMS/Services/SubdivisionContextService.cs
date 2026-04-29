namespace HRMS.Services;

public class SubdivisionContextService
{
    public int? SelectedSubdivisionId { get; private set; }
    public string SelectedSubdivisionName { get; private set; } = string.Empty;
    public event Action? OnSubdivisionChanged;

    public void SetSubdivision(int id, string name)
    {
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
