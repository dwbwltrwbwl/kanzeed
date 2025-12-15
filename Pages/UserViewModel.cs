// ViewModel для общих пользователей (клиент / сотрудник)
using System.ComponentModel;

public class UserViewModel : INotifyPropertyChanged
{
    private int _id;
    private bool _isEmployee;
    private int _roleId;
    private string _roleName;

    public int Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(nameof(Id)); }
    }

    public bool IsEmployee
    {
        get => _isEmployee;
        set { _isEmployee = value; OnPropertyChanged(nameof(IsEmployee)); }
    }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }

    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    public string Email { get; set; }

    public int RoleId
    {
        get => _roleId;
        set
        {
            if (_roleId != value)
            {
                _roleId = value;
                OnPropertyChanged(nameof(RoleId));
            }
        }
    }

    public string RoleName
    {
        get => _roleName;
        set
        {
            if (_roleName != value)
            {
                _roleName = value;
                OnPropertyChanged(nameof(RoleName));
            }
        }
    }

    // Добавим свойство для хранения предыдущей роли
    public int PreviousRoleId { get; set; }
    public bool PreviousIsEmployee { get; set; }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    #endregion
}