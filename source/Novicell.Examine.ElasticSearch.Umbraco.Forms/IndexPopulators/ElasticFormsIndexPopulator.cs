using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Umbraco.Examine;
using Umbraco.Forms.Core.Data.RecordIndex;
using Umbraco.Forms.Core.Models;
using Umbraco.Forms.Core.Persistence.Dtos;
using Umbraco.Forms.Core.Services;

namespace Novicell.Examine.ElasticSearch.Umbraco.Forms.IndexPopulators
{
    public class ElasticFormsIndexPopulator : IndexPopulator, IFormsIndexPopulator
  {
    private readonly IFormService _formService;
    private readonly IRecordService _recordService;
    private readonly IValueSetBuilder<Record> _valueSetBuilder;
    private readonly IExamineManager _examineManager;

    public ElasticFormsIndexPopulator(
      IRecordService recordService,
      IFormService formService,
      IValueSetBuilder<Record> valueSetBuilder,
      IExamineManager examineManager)
    {
      this._recordService = recordService;
      this._formService = formService;
      this._valueSetBuilder = valueSetBuilder;
      this._examineManager = examineManager;
      this.RegisterIndex("umbracoformsrecordsindex");
    }

    protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
    {
      if (!indexes.Any<IIndex>())
        return;
      foreach (Form form in this._formService.Get())
      {
        Record[] array1 = this._recordService.GetAllRecords(form).ToArray<Record>();
        if ((uint) array1.Length > 0U)
        {
          ValueSet[] array2 = this._valueSetBuilder.GetValueSets(array1).ToArray<ValueSet>();
          foreach (IIndex index in (IEnumerable<IIndex>) indexes)
            index.IndexItems((IEnumerable<ValueSet>) array2);
        }
      }
    }

    public void RemoveFromIndex(Record record) => this.HandleMediaItemsInIndex(record, (Action<Record[], IIndex>) ((records, index) =>
    {
      string[] array = ((IEnumerable<Record>) records).Select<Record, string>((Func<Record, string>) (m => m.Id.ToString())).ToArray<string>();
      if ((uint) array.Length <= 0U)
        return;
      index.DeleteFromIndex((IEnumerable<string>) array);
    }));

    public void AddOrUpdateInIndex(Record record) => this.HandleMediaItemsInIndex(record, (Action<Record[], IIndex>) ((records, index) => index.IndexItems(this._valueSetBuilder.GetValueSets(records))));

    private void HandleMediaItemsInIndex(Record record, Action<Record[], IIndex> action)
    {
      IIndex index;
      if (!this._examineManager.TryGetIndex("umbracoformsrecordsindex", out index))
        return;
      Record[] recordArray = new Record[1]{ record };
      action(recordArray, index);
    }

    void IFormsIndexPopulator.RegisterIndex(string indexName) => this.RegisterIndex(indexName);
    }
}