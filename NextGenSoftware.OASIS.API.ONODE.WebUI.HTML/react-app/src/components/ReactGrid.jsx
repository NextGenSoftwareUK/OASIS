import Paper from "@material-ui/core/Paper";
import {
    SortingState,
    IntegratedSorting,
    FilteringState,
    IntegratedFiltering,
} from "@devexpress/dx-react-grid";

import {
    Grid,
    Table,
    TableHeaderRow,
    TableFilterRow,
    TableColumnResizing,
    Toolbar,
} from "@devexpress/dx-react-grid-material-ui";
// import "../assets/scss/react-grid.scss"

const ReactGrid = ({ rows, columns, columnWidths }) => {
    return (
        <>
            <Paper>
                <Grid rows={rows} columns={columns} >
                    <SortingState />
                    <IntegratedSorting />
                    <FilteringState defaultFilters={[]} />
                    <IntegratedFiltering />
                    <Table />
                    <TableColumnResizing defaultColumnWidths={columnWidths} />
                    <TableHeaderRow showSortingControls />
                    <TableFilterRow />
                    <Toolbar />
                </Grid>
            </Paper>
        </>
    );
};

export default ReactGrid;
