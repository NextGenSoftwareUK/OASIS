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
    Toolbar,
} from "@devexpress/dx-react-grid-material-ui";

const ReactGrid = ({ rows, columns }) => {
    return (
        <>
            <Paper>
                <Grid rows={rows} columns={columns}>
                    <SortingState />
                    <IntegratedSorting />
                    <FilteringState defaultFilters={[]} />
                    <IntegratedFiltering />
                    <Table />
                    <TableHeaderRow showSortingControls />
                    <TableFilterRow />
                    <Toolbar />
                </Grid>
            </Paper>
        </>
    );
};

export default ReactGrid;
