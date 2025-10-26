using CodeGenerator.API.Models;
using System.Text;

namespace CodeGenerator.API.Services
{
    public interface IAngularCodeGenerationService
    {
        string GenerateModel(DatabaseTable table);
        string GenerateService(DatabaseTable table);
        string GenerateListComponent(DatabaseTable table);
        string GenerateFormComponent(DatabaseTable table);
        string GenerateListHtml(DatabaseTable table);
        string GenerateFormHtml(DatabaseTable table);
        string GenerateListCss(DatabaseTable table);
        string GenerateFormCss(DatabaseTable table);
    }

    public class AngularCodeGenerationService : IAngularCodeGenerationService
    {
        private readonly ILogger<AngularCodeGenerationService> _logger;

        public AngularCodeGenerationService(ILogger<AngularCodeGenerationService> logger)
        {
            _logger = logger;
        }

        public string GenerateModel(DatabaseTable table)
        {
            var className = ToPascalCase(table.TableName);
            var sb = new StringBuilder();

            sb.AppendLine($"export interface {className} {{");
            
            foreach (var column in table.Columns)
            {
                var propertyName = ToCamelCase(column.ColumnName);
                var isOptional = column.IsNullable && !column.IsPrimaryKey ? "?" : "";
                sb.AppendLine($"  {propertyName}{isOptional}: {column.TypeScriptType};");
            }
            
            sb.AppendLine("}");

            return sb.ToString();
        }

        public string GenerateService(DatabaseTable table)
        {
            var className = ToPascalCase(table.TableName);
            var serviceName = $"{className}Service";
            var interfaceName = className;
            var camelCaseName = ToCamelCase(table.TableName);
            var primaryKey = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);
            var primaryKeyType = primaryKey?.TypeScriptType ?? "number";
            var primaryKeyProperty = primaryKey?.ColumnName != null ? ToCamelCase(primaryKey.ColumnName) : "id";

            var template = $@"import {{ Injectable }} from '@angular/core';
import {{ BehaviorSubject, Observable }} from 'rxjs';
import {{ {interfaceName} }} from '../models/{camelCaseName}.model';

@Injectable({{
  providedIn: 'root'
}})
export class {serviceName} {{
  private {camelCaseName}s: {interfaceName}[] = [];
  private {camelCaseName}sSubject = new BehaviorSubject<{interfaceName}[]>([]);
  private nextId = 1;

  constructor() {{
    this.initializeSampleData();
  }}

  private initializeSampleData(): void {{
    // Add some sample data
    const sampleData: {interfaceName}[] = [
      // TODO: Add sample data based on your table structure
    ];
    
    this.{camelCaseName}s = sampleData;
    this.{camelCaseName}sSubject.next(this.{camelCaseName}s);
  }}

  get{className}s(): Observable<{interfaceName}[]> {{
    return this.{camelCaseName}sSubject.asObservable();
  }}

  get{className}ById(id: {primaryKeyType}): {interfaceName} | undefined {{
    return this.{camelCaseName}s.find(item => item.{primaryKeyProperty} === id);
  }}

  add{className}(item: Omit<{interfaceName}, '{primaryKeyProperty}'>): boolean {{
    try {{
      const new{className}: {interfaceName} = {{
        ...item,
        {primaryKeyProperty}: this.nextId++
      }} as {interfaceName};
      
      this.{camelCaseName}s.push(new{className});
      this.{camelCaseName}sSubject.next([...this.{camelCaseName}s]);
      return true;
    }} catch (error) {{
      console.error('Error adding {camelCaseName}:', error);
      return false;
    }}
  }}

  update{className}(id: {primaryKeyType}, updates: Partial<{interfaceName}>): boolean {{
    try {{
      const index = this.{camelCaseName}s.findIndex(item => item.{primaryKeyProperty} === id);
      if (index !== -1) {{
        this.{camelCaseName}s[index] = {{ ...this.{camelCaseName}s[index], ...updates }};
        this.{camelCaseName}sSubject.next([...this.{camelCaseName}s]);
        return true;
      }}
      return false;
    }} catch (error) {{
      console.error('Error updating {camelCaseName}:', error);
      return false;
    }}
  }}

  delete{className}(id: {primaryKeyType}): boolean {{
    try {{
      const index = this.{camelCaseName}s.findIndex(item => item.{primaryKeyProperty} === id);
      if (index !== -1) {{
        this.{camelCaseName}s.splice(index, 1);
        this.{camelCaseName}sSubject.next([...this.{camelCaseName}s]);
        return true;
      }}
      return false;
    }} catch (error) {{
      console.error('Error deleting {camelCaseName}:', error);
      return false;
    }}
  }}

  search{className}s(searchTerm: string): {interfaceName}[] {{
    if (!searchTerm.trim()) {{
      return this.{camelCaseName}s;
    }}
    
    const term = searchTerm.toLowerCase();
    return this.{camelCaseName}s.filter(item => {{
      // Search in all string properties
{GenerateSearchLogic(table)}
    }});
  }}

  getCategories(): string[] {{
    // TODO: Implement category logic based on your table structure
    return [];
  }}
}}";

            return template;
        }

        private string GenerateSearchLogic(DatabaseTable table)
        {
            var sb = new StringBuilder();
            var stringColumns = table.Columns.Where(c => c.TypeScriptType == "string").ToList();
            
            if (stringColumns.Any())
            {
                sb.AppendLine("      return (");
                for (int i = 0; i < stringColumns.Count; i++)
                {
                    var column = stringColumns[i];
                    var propertyName = ToCamelCase(column.ColumnName);
                    var connector = i < stringColumns.Count - 1 ? " ||" : "";
                    sb.AppendLine($"        item.{propertyName}?.toLowerCase().includes(term){connector}");
                }
                sb.AppendLine("      );");
            }
            else
            {
                sb.AppendLine("      return false;");
            }

            return sb.ToString();
        }

        public string GenerateListComponent(DatabaseTable table)
        {
            var className = ToPascalCase(table.TableName);
            var serviceName = $"{className}Service";
            var interfaceName = className;
            var camelCaseName = ToCamelCase(table.TableName);
            var componentName = $"{className}List";
            var primaryKey = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);
            var primaryKeyProperty = primaryKey?.ColumnName != null ? ToCamelCase(primaryKey.ColumnName) : "id";

            var template = $@"import {{ Component, OnInit, OnDestroy }} from '@angular/core';
import {{ CommonModule }} from '@angular/common';
import {{ FormsModule }} from '@angular/forms';
import {{ Router }} from '@angular/router';
import {{ Subscription }} from 'rxjs';
import {{ {serviceName} }} from '../../services/{camelCaseName}.service';
import {{ {interfaceName} }} from '../../models/{camelCaseName}.model';
import {{ Header }} from '../header/header';
import {{ Sidebar }} from '../sidebar/sidebar';
import {{ Footer }} from '../footer/footer';

@Component({{
  selector: 'app-{camelCaseName}-list',
  standalone: true,
  imports: [CommonModule, FormsModule, Header, Sidebar, Footer],
  templateUrl: './{camelCaseName}-list.html',
  styleUrl: './{camelCaseName}-list.css'
}})
export class {componentName} implements OnInit, OnDestroy {{
  {camelCaseName}s: {interfaceName}[] = [];
  filtered{className}s: {interfaceName}[] = [];
  searchTerm: string = '';
  selectedCategory: string = '';
  categories: string[] = [];
  
  // Pagination
  currentPage: number = 1;
  itemsPerPage: number = 10;
  totalItems: number = 0;
  
  // Sorting
  sortField: keyof {interfaceName} = '{primaryKeyProperty}';
  sortDirection: 'asc' | 'desc' = 'asc';
  
  private subscription: Subscription = new Subscription();

  constructor(
    private {camelCaseName}Service: {serviceName},
    private router: Router
  ) {{}}

  ngOnInit(): void {{
    this.load{className}s();
    this.loadCategories();
  }}

  ngOnDestroy(): void {{
    this.subscription.unsubscribe();
  }}

  load{className}s(): void {{
    this.subscription.add(
      this.{camelCaseName}Service.get{className}s().subscribe({camelCaseName}s => {{
        this.{camelCaseName}s = {camelCaseName}s;
        this.applyFiltersAndSort();
      }})
    );
  }}

  loadCategories(): void {{
    this.categories = ['All', ...this.{camelCaseName}Service.getCategories()];
  }}

  applyFiltersAndSort(): void {{
    let filtered = [...this.{camelCaseName}s];

    // Apply search filter
    if (this.searchTerm.trim()) {{
      filtered = this.{camelCaseName}Service.search{className}s(this.searchTerm);
    }}

    // Apply category filter
    if (this.selectedCategory && this.selectedCategory !== 'All') {{
      // TODO: Implement category filtering based on your table structure
    }}

    // Apply sorting
    filtered.sort((a, b) => {{
      const aValue = a[this.sortField];
      const bValue = b[this.sortField];

      if (aValue == null && bValue == null) {{
        return 0;
      }}
      if (aValue == null) {{
        return 1;
      }}
      if (bValue == null) {{
        return -1;
      }}
      
      let comparison = 0;
      if (aValue < bValue) comparison = -1;
      else if (aValue > bValue) comparison = 1;
      
      return this.sortDirection === 'desc' ? -comparison : comparison;
    }});

    this.filtered{className}s = filtered;
    this.totalItems = filtered.length;
    this.currentPage = 1; // Reset to first page when filtering
  }}

  onSearch(): void {{
    this.applyFiltersAndSort();
  }}

  onCategoryChange(): void {{
    this.applyFiltersAndSort();
  }}

  sortBy(field: keyof {interfaceName}): void {{
    if (this.sortField === field) {{
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    }} else {{
      this.sortField = field;
      this.sortDirection = 'asc';
    }}
    this.applyFiltersAndSort();
  }}

  getSortIcon(field: keyof {interfaceName}): string {{
    if (this.sortField !== field) return '↕️';
    return this.sortDirection === 'asc' ? '↑' : '↓';
  }}

  getPaginated{className}s(): {interfaceName}[] {{
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    const endIndex = startIndex + this.itemsPerPage;
    return this.filtered{className}s.slice(startIndex, endIndex);
  }}

  getTotalPages(): number {{
    return Math.ceil(this.totalItems / this.itemsPerPage);
  }}

  goToPage(page: number): void {{
    if (page >= 1 && page <= this.getTotalPages()) {{
      this.currentPage = page;
    }}
  }}

  previousPage(): void {{
    if (this.currentPage > 1) {{
      this.currentPage--;
    }}
  }}

  nextPage(): void {{
    if (this.currentPage < this.getTotalPages()) {{
      this.currentPage++;
    }}
  }}

  add{className}(): void {{
    this.router.navigate(['/{camelCaseName}s/new']);
  }}

  edit{className}(item: {interfaceName}): void {{
    this.router.navigate(['/{camelCaseName}s/edit', item.{primaryKeyProperty}]);
  }}

  delete{className}(item: {interfaceName}): void {{
    if (confirm(`Are you sure you want to delete this {camelCaseName}?`)) {{
      if (this.{camelCaseName}Service.delete{className}(item.{primaryKeyProperty})) {{
        // Item deleted successfully, the subscription will update the list
      }} else {{
        alert('Failed to delete {camelCaseName}');
      }}
    }}
  }}

  // Pagination helper methods
  getStartIndex(): number {{
    return this.totalItems === 0 ? 0 : (this.currentPage - 1) * this.itemsPerPage + 1;
  }}

  getEndIndex(): number {{
    const endIndex = this.currentPage * this.itemsPerPage;
    return Math.min(endIndex, this.totalItems);
  }}

  getVisiblePages(): number[] {{
    const totalPages = this.getTotalPages();
    const visiblePages: number[] = [];
    
    if (totalPages <= 7) {{
      for (let i = 1; i <= totalPages; i++) {{
        visiblePages.push(i);
      }}
    }} else {{
      if (this.currentPage <= 4) {{
        for (let i = 1; i <= 5; i++) {{
          visiblePages.push(i);
        }}
        if (totalPages > 6) visiblePages.push(-1);
        visiblePages.push(totalPages);
      }} else if (this.currentPage >= totalPages - 3) {{
        visiblePages.push(1);
        if (totalPages > 6) visiblePages.push(-1);
        for (let i = totalPages - 4; i <= totalPages; i++) {{
          visiblePages.push(i);
        }}
      }} else {{
        visiblePages.push(1);
        visiblePages.push(-1);
        for (let i = this.currentPage - 1; i <= this.currentPage + 1; i++) {{
          visiblePages.push(i);
        }}
        visiblePages.push(-1);
        visiblePages.push(totalPages);
      }}
    }}
    
    return visiblePages;
  }}

  onPageSizeChange(): void {{
    this.currentPage = 1;
    this.applyFiltersAndSort();
  }}

  formatCurrency(amount: number): string {{
    return new Intl.NumberFormat('en-US', {{
      style: 'currency',
      currency: 'USD'
    }}).format(amount);
  }}
}}";

            return template;
        }

        public string GenerateFormComponent(DatabaseTable table)
        {
            var className = ToPascalCase(table.TableName);
            var serviceName = $"{className}Service";
            var interfaceName = className;
            var camelCaseName = ToCamelCase(table.TableName);
            var componentName = $"{className}Form";
            var primaryKey = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);
            var primaryKeyProperty = primaryKey?.ColumnName != null ? ToCamelCase(primaryKey.ColumnName) : "id";
            var primaryKeyType = primaryKey?.TypeScriptType ?? "number";

            var template = $@"import {{ Component, OnInit }} from '@angular/core';
import {{ CommonModule }} from '@angular/common';
import {{ ReactiveFormsModule, FormBuilder, FormGroup, Validators }} from '@angular/forms';
import {{ Router, ActivatedRoute }} from '@angular/router';
import {{ {serviceName} }} from '../../services/{camelCaseName}.service';
import {{ {interfaceName} }} from '../../models/{camelCaseName}.model';
import {{ Header }} from '../header/header';
import {{ Sidebar }} from '../sidebar/sidebar';
import {{ Footer }} from '../footer/footer';

@Component({{
  selector: 'app-{camelCaseName}-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Header, Sidebar, Footer],
  templateUrl: './{camelCaseName}-form.html',
  styleUrl: './{camelCaseName}-form.css'
}})
export class {componentName} implements OnInit {{
  {camelCaseName}Form: FormGroup;
  isEditMode = false;
  {camelCaseName}Id: {primaryKeyType} | null = null;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private {camelCaseName}Service: {serviceName},
    private router: Router,
    private route: ActivatedRoute
  ) {{
    this.{camelCaseName}Form = this.createForm();
  }}

  ngOnInit(): void {{
    this.route.params.subscribe(params => {{
      if (params['id']) {{
        this.isEditMode = true;
        this.{camelCaseName}Id = +params['id'];
        this.load{className}();
      }}
    }});
  }}

  private createForm(): FormGroup {{
    return this.fb.group({{
{GenerateFormControls(table)}
    }});
  }}

  load{className}(): void {{
    if (this.{camelCaseName}Id) {{
      const {camelCaseName} = this.{camelCaseName}Service.get{className}ById(this.{camelCaseName}Id);
      if ({camelCaseName}) {{
        this.{camelCaseName}Form.patchValue({camelCaseName});
      }} else {{
        alert('{className} not found');
        this.router.navigate(['/{camelCaseName}s']);
      }}
    }}
  }}

  onSubmit(): void {{
    if (this.{camelCaseName}Form.valid) {{
      this.isSubmitting = true;
      const formValue = this.{camelCaseName}Form.value;

      if (this.isEditMode && this.{camelCaseName}Id) {{
        const success = this.{camelCaseName}Service.update{className}(this.{camelCaseName}Id, formValue);
        if (success) {{
          alert('{className} updated successfully!');
          this.router.navigate(['/{camelCaseName}s']);
        }} else {{
          alert('Failed to update {camelCaseName}');
        }}
      }} else {{
        const success = this.{camelCaseName}Service.add{className}(formValue);
        if (success) {{
          alert('{className} created successfully!');
          this.router.navigate(['/{camelCaseName}s']);
        }} else {{
          alert('Failed to create {camelCaseName}');
        }}
      }}
      this.isSubmitting = false;
    }} else {{
      this.markFormGroupTouched();
    }}
  }}

  private markFormGroupTouched(): void {{
    Object.keys(this.{camelCaseName}Form.controls).forEach(key => {{
      const control = this.{camelCaseName}Form.get(key);
      control?.markAsTouched();
    }});
  }}

  onCancel(): void {{
    this.router.navigate(['/{camelCaseName}s']);
  }}

  getFieldError(fieldName: string): string {{
    const field = this.{camelCaseName}Form.get(fieldName);
    if (field?.errors && field.touched) {{
      if (field.errors['required']) {{
        return `${{fieldName}} is required`;
      }}
      if (field.errors['email']) {{
        return 'Please enter a valid email address';
      }}
      if (field.errors['min']) {{
        return `Minimum value is ${{field.errors['min'].min}}`;
      }}
      if (field.errors['max']) {{
        return `Maximum value is ${{field.errors['max'].max}}`;
      }}
      if (field.errors['minlength']) {{
        return `Minimum length is ${{field.errors['minlength'].requiredLength}}`;
      }}
      if (field.errors['maxlength']) {{
        return `Maximum length is ${{field.errors['maxlength'].requiredLength}}`;
      }}
    }}
    return '';
  }}
}}";

            return template;
        }

        private string GenerateFormControls(DatabaseTable table)
        {
            var sb = new StringBuilder();
            var nonIdentityColumns = table.Columns.Where(c => !c.IsIdentity).ToList();

            for (int i = 0; i < nonIdentityColumns.Count; i++)
            {
                var column = nonIdentityColumns[i];
                var propertyName = ToCamelCase(column.ColumnName);
                var validators = GenerateValidators(column);
                var comma = i < nonIdentityColumns.Count - 1 ? "," : "";
                
                sb.AppendLine($"      {propertyName}: ['{GetDefaultValue(column)}'{validators}]{comma}");
            }

            return sb.ToString();
        }

        private string GenerateValidators(DatabaseColumn column)
        {
            var validators = new List<string>();

            if (!column.IsNullable)
            {
                validators.Add("Validators.required");
            }

            if (column.MaxLength.HasValue && column.TypeScriptType == "string")
            {
                validators.Add($"Validators.maxLength({column.MaxLength})");
            }

            if (column.TypeScriptType == "string" && column.ColumnName.ToLower().Contains("email"))
            {
                validators.Add("Validators.email");
            }

            if (column.TypeScriptType == "number")
            {
                validators.Add("Validators.min(0)");
            }

            return validators.Any() ? $", [{string.Join(", ", validators)}]" : "";
        }

        private string GetDefaultValue(DatabaseColumn column)
        {
            return column.TypeScriptType switch
            {
                "string" => "",
                "number" => "0",
                "boolean" => "false",
                "Date" => "",
                _ => ""
            };
        }

        public string GenerateListHtml(DatabaseTable table)
        {
            var className = ToPascalCase(table.TableName);
            var camelCaseName = ToCamelCase(table.TableName);
            var displayColumns = table.Columns.Take(5).ToList(); // Show first 5 columns in table

            var template = $@"<div class=""page-layout"">
  <app-header></app-header>
  
  <div class=""content-wrapper"">
    <app-sidebar></app-sidebar>
    
    <main class=""main-content"">
      <div class=""{camelCaseName}-list-container"">
        <div class=""header-section"">
          <h1>{className} Management</h1>
          <button type=""button"" class=""btn btn-primary"" (click)=""add{className}()"">
            <span class=""icon"">+</span>
            Add New {className}
          </button>
        </div>

        <!-- Search and Filters -->
        <div class=""filter-section"">
          <div class=""search-bar"">
            <input 
              type=""text"" 
              class=""search-input"" 
              placeholder=""Search {camelCaseName}s..."" 
              [(ngModel)]=""searchTerm""
              (input)=""onSearch()"">
          </div>

          <div class=""filter-controls"">
            <select 
              class=""filter-select"" 
              [(ngModel)]=""selectedCategory""
              (change)=""onCategoryChange()"">
              <option value="""">All Categories</option>
              <option *ngFor=""let category of categories"" [value]=""category"">{{{{ category }}}}</option>
            </select>
          </div>
        </div>

        <!-- Data Table -->
        <div class=""table-container"">
          <table class=""data-table"">
            <thead>
              <tr>
{GenerateTableHeaders(displayColumns)}
                <th class=""actions-column"">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor=""let item of getPaginated{className}s()"" class=""table-row"">
{GenerateTableCells(displayColumns, "item")}
                <td class=""actions-cell"">
                  <button 
                    type=""button"" 
                    class=""btn btn-outline btn-sm""
                    (click)=""edit{className}(item)"">
                    Edit
                  </button>
                  <button 
                    type=""button"" 
                    class=""btn btn-danger btn-sm""
                    (click)=""delete{className}(item)"">
                    Delete
                  </button>
                </td>
              </tr>
            </tbody>
          </table>

          <div class=""empty-state"" *ngIf=""filtered{className}s.length === 0"">
            <div class=""empty-icon"">📋</div>
            <h3>No {className}s Found</h3>
            <p>{{{{ searchTerm ? 'No {camelCaseName}s match your search criteria.' : 'No {camelCaseName}s available. Add one to get started.' }}}}</p>
          </div>
        </div>

        <!-- Pagination -->
        <div class=""pagination-section"" *ngIf=""totalItems > 0"">
          <div class=""pagination-info"">
            <span>Showing {{{{ getStartIndex() }}}} to {{{{ getEndIndex() }}}} of {{{{ totalItems }}}} entries</span>
            <div class=""page-size-selector"">
              <label>Show:</label>
              <select [(ngModel)]=""itemsPerPage"" (change)=""onPageSizeChange()"">
                <option value=""10"">10</option>
                <option value=""25"">25</option>
                <option value=""50"">50</option>
                <option value=""100"">100</option>
              </select>
              <span>per page</span>
            </div>
          </div>

          <div class=""pagination-controls"">
            <button 
              type=""button"" 
              class=""btn btn-outline btn-sm""
              [disabled]=""currentPage === 1""
              (click)=""previousPage()"">
              Previous
            </button>

            <div class=""page-numbers"">
              <button 
                *ngFor=""let page of getVisiblePages()""
                type=""button""
                class=""btn btn-outline btn-sm""
                [class.active]=""page === currentPage""
                [disabled]=""page === -1""
                (click)=""page !== -1 && goToPage(page)"">
                {{{{ page === -1 ? '...' : page }}}}
              </button>
            </div>

            <button 
              type=""button"" 
              class=""btn btn-outline btn-sm""
              [disabled]=""currentPage === getTotalPages()""
              (click)=""nextPage()"">
              Next
            </button>
          </div>
        </div>
      </div>
    </main>
  </div>
  
  <app-footer></app-footer>
</div>";

            return template;
        }

        private string GenerateTableHeaders(List<DatabaseColumn> columns)
        {
            var sb = new StringBuilder();
            foreach (var column in columns)
            {
                var displayName = ToDisplayName(column.ColumnName);
                var propertyName = ToCamelCase(column.ColumnName);
                sb.AppendLine($"                <th class=\"sortable-header\" (click)=\"sortBy('{propertyName}')\">");
                sb.AppendLine($"                  {displayName}");
                sb.AppendLine($"                  <span class=\"sort-icon\">{{{{ getSortIcon('{propertyName}') }}}}</span>");
                sb.AppendLine("                </th>");
            }
            return sb.ToString();
        }

        private string GenerateTableCells(List<DatabaseColumn> columns, string itemName)
        {
            var sb = new StringBuilder();
            foreach (var column in columns)
            {
                var propertyName = ToCamelCase(column.ColumnName);
                var cellContent = GenerateCellContent(column, itemName, propertyName);
                sb.AppendLine($"                <td>{cellContent}</td>");
            }
            return sb.ToString();
        }

        private string GenerateCellContent(DatabaseColumn column, string itemName, string propertyName)
        {
            return column.TypeScriptType switch
            {
        "number" when column.ColumnName.ToLower().Contains("price") || column.ColumnName.ToLower().Contains("amount") || column.ColumnName.ToLower().Contains("cost") 
          => $"{{{{ formatCurrency({itemName}.{propertyName}) }}}}",
        "Date" => $"{{{{ {itemName}.{propertyName} | date:'short' }}}}",
        "boolean" => $"<span class=\"status-badge\" [class.status-active]=\"{itemName}.{propertyName}\">{{{{ {itemName}.{propertyName} ? 'Yes' : 'No' }}}}</span>",
        _ => $"{{{{ {itemName}.{propertyName} }}}}"
            };
        }

        public string GenerateFormHtml(DatabaseTable table)
        {
            var className = ToPascalCase(table.TableName);
            var camelCaseName = ToCamelCase(table.TableName);
            var nonIdentityColumns = table.Columns.Where(c => !c.IsIdentity).ToList();

            var template = $@"<div class=""page-layout"">
  <app-header></app-header>
  
  <div class=""content-wrapper"">
    <app-sidebar></app-sidebar>
    
    <main class=""main-content"">
      <div class=""{camelCaseName}-form-container"">
        <div class=""header-section"">
          <h1>{{{{ isEditMode ? 'Edit' : 'Add New' }}}} {className}</h1>
        </div>

        <form [formGroup]=""{camelCaseName}Form"" (ngSubmit)=""onSubmit()"" class=""{camelCaseName}-form"">
          <div class=""form-grid"">
{GenerateFormFields(nonIdentityColumns)}
          </div>

          <div class=""form-actions"">
            <button 
              type=""button"" 
              class=""btn btn-secondary""
              (click)=""onCancel()""
              [disabled]=""isSubmitting"">
              Cancel
            </button>
            <button 
              type=""submit"" 
              class=""btn btn-primary""
              [disabled]=""{camelCaseName}Form.invalid || isSubmitting"">
              {{{{ isSubmitting ? 'Saving...' : (isEditMode ? 'Update' : 'Create') }}}} {className}
            </button>
          </div>
        </form>
      </div>
    </main>
  </div>
  
  <app-footer></app-footer>
</div>";

            return template;
        }

        private string GenerateFormFields(List<DatabaseColumn> columns)
        {
            var sb = new StringBuilder();
            
            foreach (var column in columns)
            {
                var propertyName = ToCamelCase(column.ColumnName);
                var displayName = ToDisplayName(column.ColumnName);
                var inputType = GetInputType(column);
                var isRequired = !column.IsNullable;

                sb.AppendLine($"            <div class=\"form-group\">");
                sb.AppendLine($"              <label for=\"{propertyName}\" class=\"form-label\">");
                sb.AppendLine($"                {displayName}{(isRequired ? " *" : "")}");
                sb.AppendLine("              </label>");
                
                if (inputType == "textarea")
                {
                    sb.AppendLine($"              <textarea");
                    sb.AppendLine($"                id=\"{propertyName}\"");
                    sb.AppendLine($"                formControlName=\"{propertyName}\"");
                    sb.AppendLine($"                class=\"form-input\"");
                    sb.AppendLine($"                placeholder=\"Enter {displayName.ToLower()}\"");
                    sb.AppendLine($"                rows=\"3\">");
                    sb.AppendLine("              </textarea>");
                }
                else if (inputType == "select")
                {
                    sb.AppendLine($"              <select");
                    sb.AppendLine($"                id=\"{propertyName}\"");
                    sb.AppendLine($"                formControlName=\"{propertyName}\"");
                    sb.AppendLine($"                class=\"form-input\">");
                    sb.AppendLine($"                <option value=\"\">Select {displayName}</option>");
                    sb.AppendLine("                <!-- Add options here -->");
                    sb.AppendLine("              </select>");
                }
                else
                {
                    sb.AppendLine($"              <input");
                    sb.AppendLine($"                type=\"{inputType}\"");
                    sb.AppendLine($"                id=\"{propertyName}\"");
                    sb.AppendLine($"                formControlName=\"{propertyName}\"");
                    sb.AppendLine($"                class=\"form-input\"");
                    sb.AppendLine($"                placeholder=\"Enter {displayName.ToLower()}\">");
                }

                sb.AppendLine($"              <div class=\"field-error\" *ngIf=\"getFieldError('{propertyName}')\">");
                sb.AppendLine($"                {{{{ getFieldError('{propertyName}') }}}}");
                sb.AppendLine("              </div>");
                sb.AppendLine("            </div>");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GetInputType(DatabaseColumn column)
        {
            var columnName = column.ColumnName.ToLower();
            
            if (columnName.Contains("email"))
                return "email";
            if (columnName.Contains("password"))
                return "password";
            if (columnName.Contains("phone") || columnName.Contains("tel"))
                return "tel";
            if (columnName.Contains("url") || columnName.Contains("website"))
                return "url";
            if (columnName.Contains("description") || columnName.Contains("comment") || columnName.Contains("note"))
                return "textarea";
            if (columnName.Contains("category") || columnName.Contains("status") || columnName.Contains("type"))
                return "select";

            return column.TypeScriptType switch
            {
                "number" => "number",
                "boolean" => "checkbox",
                "Date" when columnName.Contains("date") => "date",
                "Date" when columnName.Contains("time") => "datetime-local",
                _ => "text"
            };
        }

        public string GenerateListCss(DatabaseTable table)
        {
            var camelCaseName = ToCamelCase(table.TableName);
            
            return $@".page-layout {{
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}}

.content-wrapper {{
  display: flex;
  flex: 1;
}}

.main-content {{
  flex: 1;
  padding: 2rem;
  background-color: #f8f9fa;
  margin-left: 250px;
}}

@media (max-width: 768px) {{
  .main-content {{
    margin-left: 0;
    padding: 1rem;
  }}
}}

.{camelCaseName}-list-container {{
  max-width: 1200px;
  margin: 0 auto;
}}

.header-section {{
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  gap: 1rem;
}}

.header-section h1 {{
  color: #2c3e50;
  margin: 0;
  font-size: 2rem;
  font-weight: 600;
}}

.filter-section {{
  background: #fff;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
  display: flex;
  gap: 1rem;
  align-items: center;
  flex-wrap: wrap;
}}

.search-bar {{
  flex: 1;
  min-width: 250px;
}}

.search-input {{
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}}

.search-input:focus {{
  outline: none;
  border-color: #007bff;
  box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
}}

.filter-controls {{
  display: flex;
  gap: 1rem;
  align-items: center;
}}

.filter-select {{
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: white;
}}

.table-container {{
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  margin-bottom: 1.5rem;
}}

.data-table {{
  width: 100%;
  border-collapse: collapse;
}}

.data-table th,
.data-table td {{
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #e9ecef;
}}

.data-table th {{
  background: #f8f9fa;
  font-weight: 600;
  color: #495057;
}}

.sortable-header {{
  cursor: pointer;
  user-select: none;
  transition: background-color 0.2s;
}}

.sortable-header:hover {{
  background: #e9ecef;
}}

.sort-icon {{
  margin-left: 0.5rem;
  color: #6c757d;
}}

.table-row:hover {{
  background: #f8f9fa;
}}

.actions-cell {{
  display: flex;
  gap: 0.5rem;
}}

.status-badge {{
  padding: 0.25rem 0.5rem;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 500;
  background: #dc3545;
  color: white;
}}

.status-active {{
  background: #28a745;
}}

.btn {{
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  transition: all 0.2s;
}}

.btn:disabled {{
  opacity: 0.6;
  cursor: not-allowed;
}}

.btn-primary {{
  background: #007bff;
  color: white;
}}

.btn-primary:hover:not(:disabled) {{
  background: #0056b3;
}}

.btn-secondary {{
  background: #6c757d;
  color: white;
}}

.btn-outline {{
  background: transparent;
  color: #007bff;
  border: 1px solid #007bff;
}}

.btn-outline:hover:not(:disabled) {{
  background: #007bff;
  color: white;
}}

.btn-danger {{
  background: #dc3545;
  color: white;
}}

.btn-danger:hover:not(:disabled) {{
  background: #c82333;
}}

.btn-sm {{
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
}}

.empty-state {{
  text-align: center;
  padding: 3rem;
  color: #6c757d;
}}

.empty-icon {{
  font-size: 3rem;
  margin-bottom: 1rem;
}}

.pagination-section {{
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff;
  padding: 1rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  flex-wrap: wrap;
  gap: 1rem;
}}

.pagination-info {{
  display: flex;
  align-items: center;
  gap: 2rem;
  color: #6c757d;
}}

.page-size-selector {{
  display: flex;
  align-items: center;
  gap: 0.5rem;
}}

.page-size-selector select {{
  padding: 0.25rem 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
}}

.pagination-controls {{
  display: flex;
  align-items: center;
  gap: 0.5rem;
}}

.page-numbers {{
  display: flex;
  gap: 0.25rem;
}}

.btn.active {{
  background: #007bff;
  color: white;
}}

@media (max-width: 768px) {{
  .header-section {{
    flex-direction: column;
    align-items: stretch;
  }}

  .filter-section {{
    flex-direction: column;
    align-items: stretch;
  }}

  .data-table {{
    font-size: 0.875rem;
  }}

  .data-table th,
  .data-table td {{
    padding: 0.5rem;
  }}

  .actions-cell {{
    flex-direction: column;
  }}

  .pagination-section {{
    flex-direction: column;
  }}

  .pagination-controls {{
    justify-content: center;
  }}
}}";
        }

        public string GenerateFormCss(DatabaseTable table)
        {
            var camelCaseName = ToCamelCase(table.TableName);
            
            return $@".page-layout {{
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}}

.content-wrapper {{
  display: flex;
  flex: 1;
}}

.main-content {{
  flex: 1;
  padding: 2rem;
  background-color: #f8f9fa;
  margin-left: 250px;
}}

@media (max-width: 768px) {{
  .main-content {{
    margin-left: 0;
    padding: 1rem;
  }}
}}

.{camelCaseName}-form-container {{
  max-width: 800px;
  margin: 0 auto;
}}

.header-section {{
  margin-bottom: 2rem;
}}

.header-section h1 {{
  color: #2c3e50;
  margin: 0;
  font-size: 2rem;
  font-weight: 600;
}}

.{camelCaseName}-form {{
  background: #fff;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}}

.form-grid {{
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}}

.form-group {{
  display: flex;
  flex-direction: column;
}}

.form-label {{
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #495057;
}}

.form-input {{
  padding: 0.75rem;
  border: 1px solid #ced4da;
  border-radius: 4px;
  font-size: 1rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}}

.form-input:focus {{
  outline: none;
  border-color: #007bff;
  box-shadow: 0 0 0 2px rgba(0, 123, 255, 0.25);
}}

.form-input.ng-invalid.ng-touched {{
  border-color: #dc3545;
}}

.field-error {{
  color: #dc3545;
  font-size: 0.875rem;
  margin-top: 0.25rem;
}}

.form-actions {{
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
  padding-top: 1.5rem;
  border-top: 1px solid #e9ecef;
}}

.btn {{
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  transition: all 0.2s;
}}

.btn:disabled {{
  opacity: 0.6;
  cursor: not-allowed;
}}

.btn-primary {{
  background: #007bff;
  color: white;
}}

.btn-primary:hover:not(:disabled) {{
  background: #0056b3;
}}

.btn-secondary {{
  background: #6c757d;
  color: white;
}}

.btn-secondary:hover:not(:disabled) {{
  background: #545b62;
}}

@media (max-width: 768px) {{
  .form-grid {{
    grid-template-columns: 1fr;
  }}

  .form-actions {{
    flex-direction: column-reverse;
  }}

  .btn {{
    justify-content: center;
  }}
}}

/* Custom styles for specific input types */
input[type=""checkbox""] {{
  width: auto;
  margin-right: 0.5rem;
}}

textarea.form-input {{
  resize: vertical;
  min-height: 100px;
}}

select.form-input {{
  background-image: url(""data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='m6 8 4 4 4-4'/%3e%3c/svg%3e"");
  background-position: right 0.5rem center;
  background-repeat: no-repeat;
  background-size: 1.5em 1.5em;
  padding-right: 2.5rem;
}}";
        }

        private string ToPascalCase(string input)
        {
            return string.Join("", input.Split('_', '-', ' ')
                .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
        }

        private string ToCamelCase(string input)
        {
            var pascalCase = ToPascalCase(input);
            return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
        }

        private string ToDisplayName(string input)
        {
            return string.Join(" ", input.Split('_', '-')
                .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
        }
    }
}