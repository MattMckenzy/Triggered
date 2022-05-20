using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Dynamic;
using Triggered.Models;
using Triggered.Services;

namespace Triggered.Components
{
    public partial class DataList
    {
        #region Services Injection

        [Inject]
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; set; } = null!;

        [Inject]
        private DataService DataService { get; set; } = null!;

        [Inject]
        private MessagingService MessagingService { get; set; } = null!;

        #endregion

        #region Private Variables

        private IEnumerable<DataObject> DataObjects { get; set; } = Array.Empty<DataObject>();
        private Dictionary<string, string>? DataObjectNames { get; set; }
        private Dictionary<string, IEnumerable<string>>? CategoryDataObjects { get; set; }

        private DataObject CurrentDataObject { get; set; } = new(string.Empty, 0);
        private bool CurrentDataObjectIsValid = false;
        private bool CurrentDataObjectIsDirty = false;

        private readonly IList<string> InvalidJsonMessages = new List<string>();

        private ModalPrompt ModalPromptReference = null!;

        #endregion

        #region Lifecycle Methods

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await UpdatePageState();            

            await base.OnAfterRenderAsync(firstRender);
        }

        #endregion

        #region UI Events

        private async Task UpdateProperty(string propertyName, object? value)
        {     
            if (propertyName == nameof(DataObject.ExpandoObjectJson) && value is string jsonDataString && string.IsNullOrWhiteSpace(jsonDataString))
                value = null;

            if (propertyName == nameof(DataObject.Key) && value is string keyString)
                CurrentDataObject.Depth = keyString.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;

            typeof(DataObject).GetProperty(propertyName)!.SetValue(CurrentDataObject, value);

            await UpdatePageState();
        }

        private async Task CreateDataObject()
        {
            async void createAction()
            {
                await SetCurrentDataObject(new DataObject(string.Empty, 0));
            }

            if (CurrentDataObjectIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to create a new data object and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = createAction
                });
            else
                createAction();
        }

        private async Task LoadDataObject(string dataObjectKey)
        {
            if (dataObjectKey == CurrentDataObject.Key)
                return;

            DataObject? dataObject = DataObjects.FirstOrDefault(dataObjects => dataObjects.Key == dataObjectKey);

            if (dataObject == null)
                return;            

            async void loadAction()
            {
                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                await SetCurrentDataObject((DataObject)triggeredDbContext.Entry(dataObject).CurrentValues.Clone().ToObject());
            }

            if (CurrentDataObjectIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to edit the data object \"{dataObject.Key}\" and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = loadAction
                });
            else
                loadAction();
        }

        private async Task SaveDataObject()
        {
            using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            ExpandoObject? expandoObject = GetExpandoObject();

            if (expandoObject == null)
            {
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "JSON data error.",
                    Message = $"The JSON data could not be parsed. Please make sure the JSON data is accurate and try again.",
                    CancelChoice = "Dismiss"
                });

                return;
            }                
            else if (triggeredDbContext.DataObjects.Any(dataObject => dataObject.Key == CurrentDataObject.Key))
            {
                await DataService.SetObject(CurrentDataObject.Key, expandoObject);

                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "Data object updated!",
                    Message = $"Successfully updated the data object \"{CurrentDataObject.Key}\".",
                    CancelChoice = "Dismiss"
                });
            }
            else
            {
                await DataService.SetObject(CurrentDataObject.Key, expandoObject);


                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "Data object added!",
                    Message = $"Successfully added the data object \"{CurrentDataObject.Key}\".",
                    CancelChoice = "Dismiss"
                });
            }

            DataObject? dataObject = triggeredDbContext.DataObjects.FirstOrDefault(dataObjects => dataObjects.Key == CurrentDataObject.Key);

            if (dataObject != null)
                await SetCurrentDataObject((DataObject)triggeredDbContext.Entry(dataObject).CurrentValues.Clone().ToObject());
        }

        private async Task DeleteDataObject(string dataObjectKey)
        {
            DataObject? dataObject = DataObjects.FirstOrDefault(dataObject => dataObject.Key == dataObjectKey);

            if (dataObject == null)
                return;

            await ModalPromptReference.ShowModalPrompt(new()
            {
                Title = "WARNING: Data object deletion!",
                Message = $"Are you sure you want to delete the data object \"{dataObject.Key}\"?",
                CancelChoice = "Cancel",
                Choice = "Delete",
                ChoiceColour = "danger",
                ChoiceAction = async () =>
                {
                    if (CurrentDataObject.Key == dataObject.Key)
                        await CreateDataObject();

                    await DataService.RemoveObject(dataObject.Key);

                    await MessagingService.AddMessage($"Succesfully removed the data object \"{dataObject.Key}\"!", MessageCategory.Module);

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Data object removed!",
                        Message = $"Successfully removed the data object \"{dataObject.Key}\".",
                        CancelChoice = "Dismiss"
                    });

                    await UpdatePageState();
                }
            });

        }

        #endregion
        
        #region Helper Methods

        private async Task UpdatePageState()
        {
            CurrentDataObjectIsValid = GetExpandoObject() != null &&
                !string.IsNullOrWhiteSpace(CurrentDataObject.Key);

            TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            CurrentDataObjectIsDirty = (string.IsNullOrWhiteSpace(CurrentDataObject.Key) && !string.IsNullOrWhiteSpace(CurrentDataObject.ExpandoObjectJson)) ||
                (!string.IsNullOrWhiteSpace(CurrentDataObject.Key) && !(CurrentDataObject.ExpandoObjectJson ?? string.Empty).Equals(triggeredDbContext.DataObjects.FirstOrDefault(dataObject => dataObject.Key == CurrentDataObject.Key)?.ExpandoObjectJson ?? string.Empty));

            DataObjects = triggeredDbContext.DataObjects.ToList();

            DataObjectNames = DataObjects.OrderBy(dataObject => dataObject.Key).ToDictionary(
                dataObject => dataObject.Key,
                dataObject =>
                {
                    string[] keyTokens = dataObject.Key.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (keyTokens.Length == 1)
                        return dataObject.Key;
                    if (keyTokens.Length == 2)
                        return keyTokens.Last();
                    else
                        return string.Join(".", keyTokens.Skip(2));
                });

            CategoryDataObjects = DataObjects
                .OrderBy(dataObject => dataObject.Key)
                .GroupBy(dataObject => 
                {
                    string[] keyTokens = dataObject.Key.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (keyTokens.Length == 1)
                        return string.Empty;
                    if (keyTokens.Length == 2)
                        return keyTokens.First();
                    else
                        return string.Join(".", keyTokens.Take(2));
                })
                .OrderBy(group => group.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(dataObject => dataObject.Key));

            await InvokeAsync(StateHasChanged);
        }

        private ExpandoObject? GetExpandoObject()
        {
            InvalidJsonMessages.Clear();

            if (CurrentDataObject?.ExpandoObjectJson == null)
                return new ExpandoObject();

            try
            {                
                return JsonConvert.DeserializeObject<ExpandoObject>(CurrentDataObject.ExpandoObjectJson);      
            }
            catch (JsonReaderException jsonReaderException)
            {
                InvalidJsonMessages.Add(jsonReaderException.Message.Replace("ExpandoObject", "object"));
            }
            catch (JsonSerializationException jsonSerializationException)
            {
                InvalidJsonMessages.Add(jsonSerializationException.Message.Replace("ExpandoObject", "object"));
            }

            return null;
        }

        private async Task SetCurrentDataObject(DataObject dataObject)
        {
            CurrentDataObject = dataObject;
            await UpdatePageState();
        }   

        #endregion
    }
}