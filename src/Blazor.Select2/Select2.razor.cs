using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Select2.Models;

namespace Select2
{
    public class Select2Base<TItem> : ComponentBase, IDisposable
    {
        public const string JSInteropObjectName = "select2Blazor";
        public const string JSInteropFuncInit = "init";
        public const string JSInteropFuncOnChange = "onChange";
        public const string JSInteropFuncSelect = "select";
        public const string JSInteropFuncMultipleSelect = "multipleSelect";

        public const string JSInvokableGetData = "select2Blazor_GetData";
        public const string JSInvokableOnChange = "select2Blazor_OnChange";

        private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;
        private readonly JsonSerializerOptions _jsonSerializerOptions =
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        [Inject] private IJSRuntime JSRuntime { get; set; }
        private DotNetObjectReference<Select2Base<TItem>> _elementRef;
        private bool _previousParsingAttemptFailed;
        private ValidationMessageStore _parsingValidationMessages;
        private Type _nullableUnderlyingType;

        [CascadingParameter] public EditContext CascadingEditContext { get; set; }

        [Parameter] public EditContext EditContext { get; set; }

        [Parameter] public string Id { get; set; }

        [Parameter] public bool IsDisabled { get; set; }

        [Parameter] public List<string> CssClasses { get; set; } = new List<string>();

        [Parameter] public Func<TItem, bool> IsOptionDisabled { get; set; } = item => false;

        [Parameter] public List<TItem> Data { get; set; }

        [Parameter] public Func<Select2QueryData, Task<List<TItem>>> GetPagedData { get; set; }

        [Parameter] public Func<TItem, string> OptionTemplate { get; set; }

        [Parameter] public Func<TItem, string> TextExpression { get; set; } = item => item.ToString();

        #region select2 init options
        [Parameter] public string Placeholder { get; set; } = "Select value";

        [Parameter] public bool AllowClear { get; set; }

        [Parameter] public string Theme { get; set; } = "default";

        [Parameter] public bool Multiple { get; set; } = false;

        [Parameter] public bool SelectOnClose { get; set; } = false;

        [Parameter] public bool CloseOnSelect { get; set; } = false;
        #endregion

        [Parameter] public Func<TItem, string> GroupBy { get; set; } = item => null;
        /// <summary>
        /// Gets or sets an expression that identifies the bound value.
        /// </summary>
        [Parameter] public Expression<Func<List<TItem>>> ValueExpression { get; set; }

        /// <summary>
        /// Gets or sets the value of the input. This should be used with two-way binding.
        /// </summary>
        /// <example>
        /// @bind-Value="model.PropertyName"
        /// </example>
        [Parameter]
        public List<TItem> Value { get; set; }

        /// <summary>
        /// Gets or sets a callback that updates the bound value.
        /// </summary>
        [Parameter] public EventCallback<List<TItem>> ValueChanged { get; set; }

        public void Refresh()
        {
            StateHasChanged();
        }

        /// <summary>
        /// Constructs an instance of <see cref="Select2Base{TItem}"/>.
        /// </summary>
        protected Select2Base()
        {
            _validationStateChangedHandler = (sender, eventArgs) => StateHasChanged();
        }

        protected Dictionary<string, TItem> InternallyMappedData { get; set; } = new Dictionary<string, TItem>();

        protected string FieldClass => GivenEditContext?.FieldCssClass(FieldIdentifier) ?? string.Empty;

        protected EditContext GivenEditContext { get; set; }

        /// <summary>
        /// Gets the <see cref="FieldIdentifier"/> for the bound value.
        /// </summary>
        protected FieldIdentifier FieldIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the current value of the input.
        /// </summary>
        protected List<TItem> CurrentValue
        {
            get => Value;
            set
            {
                var selectItems = new List<TItem>();
                var addedItems = value?.Except(Value ?? new List<TItem>(), EqualityComparer<TItem>.Default) ?? new List<TItem>();
                var removedItems = Value?.Except(value ?? new List<TItem>(), EqualityComparer<TItem>.Default) ?? new List<TItem>();
                var sameItems = value?.Intersect(Value ?? new List<TItem>(), EqualityComparer<TItem>.Default) ?? new List<TItem>();
                bool same = true;
                for (var index = 0; index < sameItems.Count(); index++)
                {
                    var target = sameItems.ElementAt(index);
                    if (target != null)
                    {
                        selectItems.Add(target);
                        //_ = SelectItem(target);
                    }
                }
                for (var index = 0; index < addedItems.Count(); index++)
                {
                    same = false;
                    var target = addedItems.ElementAt(index);
                    if (target != null)
                    {
                        selectItems.Add(target);
                        //_ = SelectItem(target);
                    }
                    Value.Add(target);
                }
                for (var index = 0; index < removedItems.Count(); index++)
                {
                    same = false;
                    var target = removedItems.ElementAt(index);
                    Value.Remove(target);
                }
                _ = SelectItem(selectItems);
                if (same)
                    return;
                if (value == Value)
                    return;
                GivenEditContext?.NotifyFieldChanged(FieldIdentifier);
                _ = ValueChanged.InvokeAsync(value);
            }
        }

        protected bool TryParseValueFromString(string value, out TItem result)
        {
            result = default;

            if (value == "null" || string.IsNullOrEmpty(value))
                return AllowClear;

            if (!InternallyMappedData.ContainsKey(value))
                return false;

            result = InternallyMappedData[value];
            return true;
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            _elementRef = DotNetObjectReference.Create(this);
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            FieldIdentifier = FieldIdentifier.Create(ValueExpression);
            _nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(TItem));
            GivenEditContext = EditContext ?? CascadingEditContext;
            if (GivenEditContext != null)
                GivenEditContext.OnValidationStateChanged += _validationStateChangedHandler;

            if (GetPagedData == null)
                GetPagedData = GetStaticData;

            CurrentValue = ValueExpression.Compile().Invoke();

            // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
            return base.SetParametersAsync(ParameterView.Empty);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                var options = JsonSerializer.Serialize(new Select2Options()
                {
                    Placeholder = Placeholder,
                    AllowClear = AllowClear,
                    Theme = Theme,
                    Multiple = Multiple,
                    SelectOnClose = SelectOnClose,
                    CloseOnSelect = CloseOnSelect,
                }, _jsonSerializerOptions);

                await JSRuntime.InvokeVoidAsync($"{JSInteropObjectName}.{JSInteropFuncInit}",
                    Id, _elementRef, options, $"{JSInvokableGetData}");

                if (CurrentValue != null)
                {
                    //foreach (var item in CurrentValue)
                    //    await SelectItem(item);
                    await SelectItem(CurrentValue);
                }

                await JSRuntime.InvokeVoidAsync($"{JSInteropObjectName}.{JSInteropFuncOnChange}",
                    Id, _elementRef, $"{JSInvokableOnChange}");
            }
        }

        private Task<List<TItem>> GetStaticData(Select2QueryData query)
        {
            if (query.Page != 1) 
                return Task.FromResult(default(List<TItem>));

            var data = Data;
            var searchTerm = query.Term;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                data = data
                    .Where(x => TextExpression(x).Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            return Task.FromResult(data);
        }

        private async Task SelectItem(TItem item)
        {
            var mappedItem = MapToSelect2Item(item);
            InternallyMappedData[mappedItem.Id] = item;
            await JSRuntime.InvokeVoidAsync($"{JSInteropObjectName}.{JSInteropFuncSelect}", Id, mappedItem);
        }

        private async Task SelectItem(List<TItem> items)
        {
            var mappedItems = new List<Select2Item>();
            foreach (var item in items)
            {
                var mappedItem = MapToSelect2Item(item);
                mappedItems.Add(mappedItem);
                InternallyMappedData[mappedItem.Id] = item;
            }
            await JSRuntime.InvokeVoidAsync($"{JSInteropObjectName}.{JSInteropFuncMultipleSelect}", Id, mappedItems.ToArray());
        }

        internal Select2Item MapToSelect2Item(TItem item)
        {
            var id = GetId(item);
            var select2Item = new Select2Item(id, TextExpression(item), IsOptionDisabled(item), GroupBy?.Invoke(item));
            if (OptionTemplate != null)
                select2Item.Html = OptionTemplate(item);
            if (Value != null)
                select2Item.Selected = Value.Select(v => GetId(v)).Contains(id);
            return select2Item;
        }

        [JSInvokable(JSInvokableGetData)]
        public async Task<string> Select2_GetDataWrapper(JsonElement element)
        {
            var json = element.GetRawText();
            var queryParams = JsonSerializer.Deserialize<Select2QueryParams>(json, _jsonSerializerOptions);

            var data = await GetPagedData(queryParams.Data);

            //if (!queryParams.Data.Type.Contains("append", StringComparison.OrdinalIgnoreCase))
            //    InternallyMappedData.Clear();

            var response = new Select2Response();
            if (data != null)
            {
                foreach (var item in data)
                {
                    var mappedItem = MapToSelect2Item(item);
                    if (!string.IsNullOrWhiteSpace(mappedItem.GroupName))
                    {
                        if (response.Results.FirstOrDefault(t => t is Select2Group g && g.Text == mappedItem.GroupName) is Select2Group select2Group)
                            select2Group.Children.Add(mappedItem);
                        else
                        {
                            var newGroup = new Select2Group(mappedItem.GroupName);
                            newGroup.Children.Add(mappedItem);
                            response.Results.Add(newGroup);
                        }
                    }
                    else
                    {
                        var newGroup = new Select2Group(mappedItem.Text);
                        newGroup.Children.Add(mappedItem);
                        response.Results.Add(newGroup);
                    }
                    InternallyMappedData[mappedItem.Id] = item;
                }
                response.Pagination.More = data.Count == queryParams.Data.Size;
            }

            return JsonSerializer.Serialize(response, _jsonSerializerOptions);
        }

        [JSInvokable(JSInvokableOnChange)]
        public void Change(JsonElement selectedValue, JsonElement selectedValues, JsonElement selectedValuesData)
        {
            List<string> values = new List<string>();
            if (selectedValues.ValueKind == JsonValueKind.Array)
            {
                values.AddRange(selectedValues.EnumerateArray().Select(item => item.GetString()).Where(item => !string.IsNullOrEmpty(item)));
            }    
            _parsingValidationMessages?.Clear();

            bool parsingFailed;

            var result = new List<TItem>();
            foreach (var value in values)
            {
                if (_nullableUnderlyingType != null && string.IsNullOrWhiteSpace(value))
                {
                    // Assume if it's a nullable type, null/empty inputs should correspond to default(T)
                    // Then all subclasses get nullable support almost automatically (they just have to
                    // not reject Nullable<T> based on the type itself).
                    parsingFailed = false;
                    result.Add(default);
                }
                else if (TryParseValueFromString(value, out var parsedValue))
                {
                    parsingFailed = false;
                    result.Add(parsedValue);
                }
                else
                {
                    parsingFailed = true;

                    if (_parsingValidationMessages == null)
                    {
                        _parsingValidationMessages = new ValidationMessageStore(GivenEditContext);
                    }

                    _parsingValidationMessages?.Add(FieldIdentifier, "Given value was not found");

                    // Since we're not writing to CurrentValue, we'll need to notify about modification from here
                    GivenEditContext?.NotifyFieldChanged(FieldIdentifier);
                }

                // We can skip the validation notification if we were previously valid and still are
                if (parsingFailed || _previousParsingAttemptFailed)
                {
                    GivenEditContext?.NotifyValidationStateChanged();
                    _previousParsingAttemptFailed = parsingFailed;
                }
            }
            CurrentValue = result;
        }

        private static string GetId(TItem item)
        {
            return item.GetHashCode().ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        void IDisposable.Dispose()
        {
            if (GivenEditContext != null)
            {
                GivenEditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }

            Dispose(disposing: true);
        }
    }
}
