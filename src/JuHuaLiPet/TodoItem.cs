namespace JuHuaLiPet;

internal sealed class TodoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public DateTime DueAt { get; set; } = DateTime.Now.AddMinutes(30);
    public bool IsDone { get; set; }
    public bool Notified { get; set; }

    public override string ToString()
    {
        var status = IsDone ? "已完成" : DueAt <= DateTime.Now ? "到点了" : "待提醒";
        return $"{DueAt:MM-dd HH:mm}  {Title}  ({status})";
    }
}
