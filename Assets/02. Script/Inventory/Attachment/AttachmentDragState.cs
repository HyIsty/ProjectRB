public enum AttachmentDragOrigin
{
    None,
    Inventory,
    Equipped
}

public static class AttachmentDragState
{
    public static WeaponAttachmentData CurrentAttachment { get; private set; }
    public static AttachmentDragOrigin CurrentOrigin { get; private set; } = AttachmentDragOrigin.None;

    public static bool IsDragging => CurrentAttachment != null;

    public static void BeginDrag(WeaponAttachmentData attachment, AttachmentDragOrigin origin)
    {
        CurrentAttachment = attachment;
        CurrentOrigin = origin;
    }

    public static void EndDrag()
    {
        CurrentAttachment = null;
        CurrentOrigin = AttachmentDragOrigin.None;
    }
}