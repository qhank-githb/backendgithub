namespace MinioWebBackend.Dtos.TagDTOs
{
        public class FileWithTagsDto
{
    public int Id { get; set; }
    public string? OriginalFileName { get; set; }
    public string? StoredFileName { get; set; }
    public string? BucketName { get; set; }
    public string? RelativePath { get; set; }
    public string? AbsolutePath { get; set; }
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public DateTime UploadTime { get; set; }
    public string? Uploader { get; set; }
    public List<string>? Tags { get; set; }
}

}
